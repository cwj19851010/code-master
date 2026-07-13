using System.Text.Json;
using System.Text.Encodings.Web;

var json = File.ReadAllText("../translations_export.json");
using var doc = JsonDocument.Parse(json);
var root = doc.RootElement;

var translations = root.GetProperty("translations").EnumerateArray()
    .Where(item => !item.GetProperty("key").GetString()!.ToLower().Contains("project"))
    .Select(item => item.GetRawText())
    .ToList();

var exportTime = root.GetProperty("exportTime").GetString();
var originalCount = root.GetProperty("totalKeys").GetInt32();

var result = $$"""
{
  "exportTime": "{{exportTime}}",
  "totalKeys": {{translations.Count}},
  "translations": [
{{string.Join(",\n", translations)}}
  ]
}
""";

File.WriteAllText("../translations_clean.json", result);

Console.WriteLine($"清理完成");
Console.WriteLine($"原始键数量: {originalCount}");
Console.WriteLine($"清理后键数量: {translations.Count}");
Console.WriteLine($"删除了 {originalCount - translations.Count} 个项目相关的翻译键");
