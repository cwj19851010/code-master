using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using CodeMaster.Application.Dtos.Auth;
using CodeMaster.Core.Repositories;
using CodeMaster.Domain.Entities.System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SqlSugar;
using Yitter.IdGenerator;

namespace CodeMaster.Application.Services.Auth;

public interface IPublicAccountService
{
    Task SendRegisterCodeAsync(SendEmailCodeDto dto);

    Task<LoginResultDto> RegisterWithEmailAsync(RegisterWithEmailDto dto);

    GithubAuthorizeUrlDto GetGithubAuthorizeUrl();

    Task<LoginResultDto> SignInWithGithubAsync(GithubCallbackDto dto);
}

public class PublicAccountService : IPublicAccountService
{
    private const string GithubProvider = "GitHub";
    private const string TenantOwnerRoleKey = "tenant_owner";

    private readonly IRepository<SysUser> _userRepository;
    private readonly IEmailSender _emailSender;
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PublicAccountService> _logger;
    private readonly ISqlSugarClient _db;

    public PublicAccountService(
        IRepository<SysUser> userRepository,
        IEmailSender emailSender,
        IJwtService jwtService,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PublicAccountService> logger,
        ISqlSugarClient db)
    {
        _userRepository = userRepository;
        _emailSender = emailSender;
        _jwtService = jwtService;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _db = db;
    }

    public async Task SendRegisterCodeAsync(SendEmailCodeDto dto)
    {
        var email = NormalizeEmail(dto.Email);
        if (await EmailExistsAsync(email))
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        var latest = await _db.Queryable<SysEmailVerification>()
            .Where(x => x.Email == email && x.Purpose == dto.Purpose && x.VerifiedAt == null)
            .OrderByDescending(x => x.CreateTime)
            .FirstAsync();

        if (latest != null && latest.CreateTime > DateTime.UtcNow.AddSeconds(-60))
        {
            throw new InvalidOperationException("Please wait before requesting another code.");
        }

        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var entity = new SysEmailVerification
        {
            Id = YitIdHelper.NextId(),
            Email = email,
            Purpose = dto.Purpose,
            CodeHash = HashCode(email, dto.Purpose, code),
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IpAddress = GetClientIp(),
            CreateTime = DateTime.UtcNow
        };

        await _db.Insertable(entity).ExecuteCommandAsync();
        try
        {
            await _emailSender.SendVerificationCodeAsync(email, code, dto.Purpose);
        }
        catch
        {
            await _db.Deleteable<SysEmailVerification>().Where(x => x.Id == entity.Id).ExecuteCommandAsync();
            throw;
        }
    }

    public async Task<LoginResultDto> RegisterWithEmailAsync(RegisterWithEmailDto dto)
    {
        var email = NormalizeEmail(dto.Email);
        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
        {
            throw new InvalidOperationException("Password must be at least 6 characters.");
        }

        if (await EmailExistsAsync(email))
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        var verified = await VerifyCodeAsync(email, "register", dto.Code);
        if (!verified)
        {
            throw new InvalidOperationException("Invalid or expired verification code.");
        }

        var tenantSetup = await CreatePublicTenantAsync(
            string.IsNullOrWhiteSpace(dto.NickName) ? email.Split('@')[0] : dto.NickName.Trim(),
            email.Split('@')[0]);

        var user = new SysUser
        {
            Id = YitIdHelper.NextId(),
            UserName = BuildUserName(email),
            NickName = string.IsNullOrWhiteSpace(dto.NickName) ? email.Split('@')[0] : dto.NickName.Trim(),
            Email = email,
            EmailConfirmed = true,
            Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            UserType = "member",
            Status = 0,
            TenantId = tenantSetup.Tenant.Id,
            DeptId = tenantSetup.Dept.Id,
            PostId = tenantSetup.Post.Id,
            CreateTime = DateTime.UtcNow
        };

        await _userRepository.InsertAsync(user);
        await AssignMemberRoleAsync(user.Id);
        await EnsurePublicUserAccessAsync(user);

        return await BuildLoginResultAsync(user);
    }

    public GithubAuthorizeUrlDto GetGithubAuthorizeUrl()
    {
        var clientId = _configuration["Authentication:GitHub:ClientId"];
        var callbackUrl = GetGithubCallbackUrl();
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException("GitHub OAuth requires Authentication:GitHub:ClientId.");
        }

        var state = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = clientId,
            ["redirect_uri"] = callbackUrl,
            ["scope"] = "read:user user:email",
            ["state"] = state
        };

        var url = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(
            "https://github.com/login/oauth/authorize",
            query);

        return new GithubAuthorizeUrlDto { AuthorizeUrl = url, State = state };
    }

    public async Task<LoginResultDto> SignInWithGithubAsync(GithubCallbackDto dto)
    {
        var clientId = _configuration["Authentication:GitHub:ClientId"];
        var clientSecret = _configuration["Authentication:GitHub:ClientSecret"];
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException("GitHub OAuth requires client id and client secret.");
        }

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("CodeMaster");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");

        var tokenResponse = await client.PostAsJsonAsync("https://github.com/login/oauth/access_token", new
        {
            client_id = clientId,
            client_secret = clientSecret,
            code = dto.Code,
            redirect_uri = GetGithubCallbackUrl(),
            state = dto.State
        });
        tokenResponse.EnsureSuccessStatusCode();
        var token = await tokenResponse.Content.ReadFromJsonAsync<GithubTokenResponse>();
        if (token == null || string.IsNullOrWhiteSpace(token.AccessToken))
        {
            throw new InvalidOperationException("GitHub OAuth did not return an access token.");
        }

        client.DefaultRequestHeaders.Authorization = new global::System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);
        var githubUser = await client.GetFromJsonAsync<GithubUserResponse>("https://api.github.com/user")
            ?? throw new InvalidOperationException("Failed to load GitHub user profile.");
        var emails = await client.GetFromJsonAsync<List<GithubEmailResponse>>("https://api.github.com/user/emails") ?? new();
        var verifiedEmail = emails.FirstOrDefault(e => e.Primary && e.Verified)?.Email
            ?? emails.FirstOrDefault(e => e.Verified)?.Email
            ?? githubUser.Email;

        var externalLogin = await _db.Queryable<SysExternalLogin>()
            .Where(x => x.Provider == GithubProvider && x.ProviderUserId == githubUser.Id.ToString())
            .FirstAsync();

        SysUser? user = null;
        if (externalLogin != null)
        {
            user = await _userRepository.GetByIdAsync(externalLogin.UserId);
        }

        if (user == null && !string.IsNullOrWhiteSpace(verifiedEmail))
        {
            var normalizedEmail = NormalizeEmail(verifiedEmail);
            user = await _db.Queryable<SysUser>()
                .Where(x => x.Email == normalizedEmail && x.EmailConfirmed)
                .FirstAsync();
        }

        if (user == null)
        {
            user = await CreateGithubUserAsync(githubUser, verifiedEmail);
        }

        if (externalLogin == null)
        {
            externalLogin = new SysExternalLogin
            {
                Id = YitIdHelper.NextId(),
                UserId = user.Id,
                Provider = GithubProvider,
                ProviderUserId = githubUser.Id.ToString(),
                ProviderLogin = githubUser.Login,
                ProviderEmail = verifiedEmail,
                AvatarUrl = githubUser.AvatarUrl,
                LastLoginTime = DateTime.UtcNow,
                CreateTime = DateTime.UtcNow
            };
            await _db.Insertable(externalLogin).ExecuteCommandAsync();
        }
        else
        {
            externalLogin.ProviderLogin = githubUser.Login;
            externalLogin.ProviderEmail = verifiedEmail;
            externalLogin.AvatarUrl = githubUser.AvatarUrl;
            externalLogin.LastLoginTime = DateTime.UtcNow;
            await _db.Updateable(externalLogin)
                .UpdateColumns(x => new { x.ProviderLogin, x.ProviderEmail, x.AvatarUrl, x.LastLoginTime })
                .ExecuteCommandAsync();
        }

        await EnsurePublicUserAccessAsync(user);
        return await BuildLoginResultAsync(user);
    }

    private async Task<SysUser> CreateGithubUserAsync(GithubUserResponse githubUser, string? verifiedEmail)
    {
        var email = string.IsNullOrWhiteSpace(verifiedEmail) ? null : NormalizeEmail(verifiedEmail);
        var tenantSetup = await CreatePublicTenantAsync(
            string.IsNullOrWhiteSpace(githubUser.Name) ? githubUser.Login : githubUser.Name,
            githubUser.Login);
        var user = new SysUser
        {
            Id = YitIdHelper.NextId(),
            UserName = await BuildUniqueUserNameAsync($"github_{githubUser.Login}"),
            NickName = string.IsNullOrWhiteSpace(githubUser.Name) ? githubUser.Login : githubUser.Name,
            Email = email,
            EmailConfirmed = !string.IsNullOrWhiteSpace(email),
            Avatar = githubUser.AvatarUrl,
            Password = BCrypt.Net.BCrypt.HashPassword(Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))),
            UserType = "member",
            Status = 0,
            TenantId = tenantSetup.Tenant.Id,
            DeptId = tenantSetup.Dept.Id,
            PostId = tenantSetup.Post.Id,
            CreateTime = DateTime.UtcNow
        };

        await _userRepository.InsertAsync(user);
        await AssignMemberRoleAsync(user.Id);
        await EnsurePublicUserAccessAsync(user);
        return user;
    }

    private async Task<(SysTenant Tenant, SysDept Dept, SysPost Post)> CreatePublicTenantAsync(string tenantNameSeed, string tenantCodeSeed)
    {
        var tenantName = string.IsNullOrWhiteSpace(tenantNameSeed)
            ? "CodeMaster User"
            : tenantNameSeed.Trim();
        var tenantCode = await BuildUniqueTenantCodeAsync(tenantCodeSeed);

        var tenant = new SysTenant
        {
            Id = YitIdHelper.NextId(),
            TenantName = tenantName,
            TenantCode = tenantCode,
            IsolationType = 1,
            Status = 0,
            TenantId = 0,
            CreateTime = DateTime.UtcNow,
            Remark = "Public registration tenant"
        };

        var dept = new SysDept
        {
            Id = YitIdHelper.NextId(),
            Name = "默认部门",
            ParentId = null,
            Ancestors = "0",
            TenantId = tenant.Id,
            CreateTime = DateTime.UtcNow
        };

        var post = new SysPost
        {
            Id = YitIdHelper.NextId(),
            PostName = "默认岗位",
            DataScope = 3,
            TenantId = tenant.Id,
            CreateTime = DateTime.UtcNow,
            Remark = "Public registration default post"
        };

        await _db.Insertable(tenant).ExecuteCommandAsync();
        await _db.Insertable(dept).ExecuteCommandAsync();
        await _db.Insertable(post).ExecuteCommandAsync();

        return (tenant, dept, post);
    }

    private async Task<string> BuildUniqueTenantCodeAsync(string seed)
    {
        var normalized = new string((seed ?? string.Empty).Trim().ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '_')
            .ToArray()).Trim('_');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = "tenant";
        }

        var tenantCode = normalized;
        var index = 1;
        while (await _db.Queryable<SysTenant>().ClearFilter().Where(x => x.TenantCode == tenantCode).AnyAsync())
        {
            index++;
            tenantCode = $"{normalized}_{index}";
        }

        return tenantCode;
    }

    private async Task<LoginResultDto> BuildLoginResultAsync(SysUser user)
    {
        var roleIds = await _db.Queryable<SysUserRole>()
            .Where(x => x.UserId == user.Id)
            .Select(x => x.RoleId)
            .ToListAsync();

        var roleList = roleIds.Count == 0
            ? new List<SysRole>()
            : await _db.Queryable<SysRole>()
                .ClearFilter()
                .Where(x => roleIds.Contains(x.Id))
                .ToListAsync();

        var roles = roleList.Select(x => x.RoleKey).ToList();
        var dataScope = roleList.Count == 0 ? 1 : roleList.Max(x => x.DataScope);
        var isHostAdmin = AdminPermissionHelper.IsHostAdmin(user, roleList);
        var isTenantAdmin = AdminPermissionHelper.IsTenantAdmin(user, roleList);
        var isAdmin = isHostAdmin || isTenantAdmin;
        var permissions = isAdmin
            ? await AdminPermissionHelper.GetScopedPermissionsAsync(_db, user.TenantId)
            : await GetPermissionsByRoleIdsAsync(roleIds, user.TenantId);

        var token = _jwtService.GenerateToken(
            user,
            roles,
            permissions,
            dataScope,
            user.DeptId,
            null,
            dataScope,
            isAdmin,
            isHostAdmin,
            isTenantAdmin);
        return new LoginResultDto
        {
            AccessToken = token,
            TokenType = "Bearer",
            ExpiresIn = Convert.ToInt32(_configuration["Jwt:ExpireHours"] ?? _configuration["JwtSettings:ExpireHours"] ?? "2") * 3600,
            UserInfo = new UserInfoDto
            {
                UserId = user.Id,
                UserName = user.UserName,
                NickName = user.NickName,
                Avatar = user.Avatar,
                Roles = roles,
                Permissions = permissions,
                DataScope = dataScope,
                IsAdmin = isAdmin,
                IsHostAdmin = isHostAdmin,
                IsTenantAdmin = isTenantAdmin
            }
        };
    }

    private async Task<bool> VerifyCodeAsync(string email, string purpose, string code)
    {
        var hash = HashCode(email, purpose, code);
        var record = await _db.Queryable<SysEmailVerification>()
            .Where(x => x.Email == email && x.Purpose == purpose && x.CodeHash == hash && x.VerifiedAt == null)
            .OrderByDescending(x => x.CreateTime)
            .FirstAsync();
        if (record == null || record.ExpiresAt < DateTime.UtcNow)
        {
            return false;
        }

        record.VerifiedAt = DateTime.UtcNow;
        await _db.Updateable(record).UpdateColumns(x => new { x.VerifiedAt }).ExecuteCommandAsync();
        return true;
    }

    private async Task AssignMemberRoleAsync(long userId)
    {
        var memberRole = await _db.Queryable<SysRole>()
            .ClearFilter()
            .Where(x => x.RoleKey == "member")
            .FirstAsync();
        if (memberRole == null)
        {
            _logger.LogWarning("Member role is not configured. Public user {UserId} will be created without a role.", userId);
            return;
        }

        var exists = await _db.Queryable<SysUserRole>()
            .Where(x => x.UserId == userId && x.RoleId == memberRole.Id)
            .AnyAsync();
        if (!exists)
        {
            await _db.Insertable(new SysUserRole { UserId = userId, RoleId = memberRole.Id }).ExecuteCommandAsync();
        }
    }

    private async Task EnsurePublicUserAccessAsync(SysUser user)
    {
        if (user.TenantId <= 0)
        {
            return;
        }

        var ownerRole = await _db.Queryable<SysRole>()
            .ClearFilter()
            .Where(x => x.TenantId == user.TenantId && x.RoleKey == TenantOwnerRoleKey)
            .FirstAsync();

        if (ownerRole == null)
        {
            ownerRole = new SysRole
            {
                Id = YitIdHelper.NextId(),
                RoleName = "租户管理员",
                RoleKey = TenantOwnerRoleKey,
                RoleSort = 1,
                Status = 0,
                DataScope = 3,
                IsTenantAdmin = true,
                TenantId = user.TenantId,
                CreateTime = DateTime.UtcNow,
                Remark = "Public registration tenant owner role"
            };
            await _db.Insertable(ownerRole).ExecuteCommandAsync();
        }
        else if (!ownerRole.IsTenantAdmin)
        {
            ownerRole.IsTenantAdmin = true;
            await _db.Updateable(ownerRole)
                .UpdateColumns(x => new { x.IsTenantAdmin })
                .ExecuteCommandAsync();
        }

        var hasOwnerRole = await _db.Queryable<SysUserRole>()
            .Where(x => x.UserId == user.Id && x.RoleId == ownerRole.Id)
            .AnyAsync();
        if (!hasOwnerRole)
        {
            await _db.Insertable(new SysUserRole { UserId = user.Id, RoleId = ownerRole.Id }).ExecuteCommandAsync();
        }

        await EnsureRoleMenusAsync(ownerRole.Id);
    }

    private async Task EnsureRoleMenusAsync(long roleId)
    {
        var menuIds = await _db.Queryable<SysMenu>()
            .ClearFilter()
            .Where(x => x.Status == 0 && (x.MenuScope == 1 || x.MenuScope == 2))
            .Select(x => x.Id)
            .ToListAsync();

        if (menuIds.Count == 0)
        {
            return;
        }

        var existingMenuIds = await _db.Queryable<SysRoleMenu>()
            .Where(x => x.RoleId == roleId)
            .Select(x => x.MenuId)
            .ToListAsync();
        var existingSet = existingMenuIds.ToHashSet();

        var roleMenus = menuIds
            .Where(menuId => !existingSet.Contains(menuId))
            .Select(menuId => new SysRoleMenu { RoleId = roleId, MenuId = menuId })
            .ToList();

        if (roleMenus.Count > 0)
        {
            await _db.Insertable(roleMenus).ExecuteCommandAsync();
        }
    }

    private async Task<List<string>> GetPermissionsByRoleIdsAsync(List<long> roleIds, long tenantId)
    {
        if (roleIds.Count == 0)
        {
            return new List<string>();
        }

        var menuIds = await _db.Queryable<SysRoleMenu>()
            .Where(x => roleIds.Contains(x.RoleId))
            .Select(x => x.MenuId)
            .ToListAsync();
        if (menuIds.Count == 0)
        {
            return new List<string>();
        }

        return await _db.Queryable<SysMenu>()
            .ClearFilter()
            .Where(x => menuIds.Contains(x.Id)
                && !string.IsNullOrEmpty(x.Perms)
                && (tenantId == 0
                    ? x.MenuScope == AdminPermissionHelper.HostMenuScope || x.MenuScope == AdminPermissionHelper.SharedMenuScope
                    : x.MenuScope == AdminPermissionHelper.TenantMenuScope || x.MenuScope == AdminPermissionHelper.SharedMenuScope))
            .Select(x => x.Perms!)
            .Distinct()
            .ToListAsync();
    }

    private async Task<bool> EmailExistsAsync(string email)
    {
        return await _db.Queryable<SysUser>()
            .ClearFilter()
            .Where(x => x.Email == email)
            .AnyAsync();
    }

    private string HashCode(string email, string purpose, string code)
    {
        var secret = _configuration["Email:CodeSecret"]
            ?? _configuration["Jwt:SecretKey"]
            ?? _configuration["JwtSettings:SecretKey"]
            ?? "CodeMaster";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{email}:{purpose}:{code}:{secret}"));
        return Convert.ToHexString(bytes);
    }

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            throw new InvalidOperationException("Invalid email address.");
        }

        return email.Trim().ToLowerInvariant();
    }

    private string BuildUserName(string email)
    {
        return BuildUniqueUserNameAsync(email.Split('@')[0]).GetAwaiter().GetResult();
    }

    private async Task<string> BuildUniqueUserNameAsync(string seed)
    {
        var normalized = new string(seed.Trim().ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '_')
            .ToArray()).Trim('_');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = "user";
        }

        var userName = normalized;
        var index = 1;
        while (await _db.Queryable<SysUser>().ClearFilter().Where(x => x.UserName == userName).AnyAsync())
        {
            index++;
            userName = $"{normalized}{index}";
        }

        return userName;
    }

    private string GetGithubCallbackUrl()
    {
        var configured = _configuration["Authentication:GitHub:CallbackUrl"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        var request = _httpContextAccessor.HttpContext?.Request;
        if (request == null)
        {
            return "/api/account/github/callback";
        }

        return $"{request.Scheme}://{request.Host}/api/account/github/callback";
    }

    private string GetClientIp()
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request == null) return "Unknown";
        return request.Headers["X-Forwarded-For"].FirstOrDefault()
            ?? request.Headers["X-Real-IP"].FirstOrDefault()
            ?? _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
            ?? "Unknown";
    }

    private sealed class GithubTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
    }

    private sealed class GithubUserResponse
    {
        public long Id { get; set; }

        public string Login { get; set; } = string.Empty;

        public string? Name { get; set; }

        public string? Email { get; set; }

        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; set; }
    }

    private sealed class GithubEmailResponse
    {
        public string Email { get; set; } = string.Empty;

        public bool Primary { get; set; }

        public bool Verified { get; set; }
    }
}
