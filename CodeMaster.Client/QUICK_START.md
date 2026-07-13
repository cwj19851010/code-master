# CodeMaster WPF Client - Quick Start

## What's Been Created

### 1. WPF Desktop Client
- **Location**: `CodeMaster.Client/`
- **Technology**: .NET 8.0 WPF + WebView2
- **Purpose**: Desktop application that embeds the Vue frontend and provides native functionality

### 2. JSBridge Communication
- **Frontend**: `CodeMaster.Vue/src/utils/jsbridge.js`
- **Backend**: `MainWindow.xaml.cs` - JsBridge class
- **Features**:
  - `isInClient()` - Detect if running in WebView
  - `clientInitializeProject()` - Initialize project locally
  - `selectFolder()` - Native folder picker
  - `showMessage()` - Native message box

### 3. Dual-Mode Architecture

#### Browser Mode (Server-side initialization)
```
User clicks "Initialize"
  ↓
Frontend calls: POST /api/project/project/initialize
  ↓
Backend initializes on server
  ↓
Returns result
```

#### Client Mode (Local initialization)
```
User clicks "Initialize"
  ↓
Frontend detects WebView environment
  ↓
Frontend calls: GET /api/project/project/getclientinitializedata/{id}
  ↓
Backend returns template Base64 (~400KB)
  ↓
Frontend calls JSBridge: clientInitializeProject(data)
  ↓
WPF client initializes locally
  ↓
Returns result
```

## How to Run

### Start Backend
```bash
cd CodeMaster.WebApi
dotnet run --urls http://localhost:5170
```

### Start WPF Client
```bash
cd CodeMaster.Client
dotnet run
```

Or run the compiled exe:
```
CodeMaster.Client\bin\Debug\net8.0-windows\CodeMaster.Client.exe
```

### Test in Browser (Server Mode)
```
Open browser: http://localhost:5170
Login: admin / admin123
Create project
Click "Initialize" - runs on server
```

### Test in Client (Local Mode)
```
Launch WPF client
Navigate to: http://localhost:5170
Login: admin / admin123
Create project
Click "Initialize" - runs locally via JSBridge
```

## Key Features

### Automatic Environment Detection
The frontend automatically detects whether it's running in a browser or WebView2 and chooses the appropriate initialization method.

### Code Reuse
Core initialization logic is shared between server and client modes:
- Extract template
- Rename files/directories
- Replace project names
- Update database config
- Run EF migrations
- Run npm install

### JSBridge Methods

#### InitializeProject(jsonData)
Initializes a project locally from Base64 template
- Decodes Base64 to bytes
- Extracts ZIP to local directory
- Renames files and replaces content
- Runs database migration
- Runs npm install

#### SelectFolder()
Opens native folder picker dialog
- Returns selected folder path
- Empty string if cancelled

#### ShowMessage(message, title)
Shows native Windows message box
- Displays message with title
- OK button only

## Project Structure

```
CodeMaster.Client/
├── MainWindow.xaml              # UI layout
├── MainWindow.xaml.cs           # Logic + JSBridge
├── CodeMaster.Client.csproj     # Project config
├── README.md                    # Detailed documentation
└── IMPLEMENTATION_SUMMARY.md    # Architecture overview
```

## Dependencies

```xml
<PackageReference Include="Microsoft.Web.WebView2" Version="1.0.2792.45" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

## Testing

### Check if JSBridge is available
Open DevTools in the client (click "开发者工具" button) and run:
```javascript
console.log(window.chrome.webview.hostObjects.jsbridge)
```

### Test JSBridge methods
```javascript
// Test message
await window.chrome.webview.hostObjects.jsbridge.ShowMessage('Hello', 'Test')

// Test folder picker
const path = await window.chrome.webview.hostObjects.jsbridge.SelectFolder()
console.log('Selected:', path)
```

## Files Modified/Created

### Backend
- ✅ `ProjectService.cs` - Added GetClientInitializeDataAsync
- ✅ `IProjectService.cs` - Added interface method
- ✅ `ProjectDto.cs` - Added ClientInitializeProjectDto
- ✅ `ProjectInitializationService.cs` - Already had dual-mode support

### Frontend
- ✅ `jsbridge.js` - New file with JSBridge utilities
- ✅ `project.js` - Added getClientInitializeData API
- ✅ `index.vue` - Modified handleInitialize for dual-mode

### Client (New Project)
- ✅ `CodeMaster.Client.csproj` - WPF project
- ✅ `MainWindow.xaml` - UI with WebView2
- ✅ `MainWindow.xaml.cs` - JSBridge implementation
- ✅ `README.md` - Documentation
- ✅ `IMPLEMENTATION_SUMMARY.md` - Architecture details

## Next Steps

1. **Test the client**: Run the WPF client and verify JSBridge works
2. **Test initialization**: Create a project and initialize it in client mode
3. **Add progress feedback**: Show initialization progress to user
4. **Error handling**: Improve error messages and recovery
5. **Logging**: Add detailed logging for debugging

## Success Criteria

- ✅ WPF client compiles without errors
- ✅ WebView2 loads the Vue frontend
- ✅ JSBridge methods are accessible from JavaScript
- ✅ Frontend automatically detects environment
- ✅ Server mode initialization works
- ⏳ Client mode initialization works (needs testing)

## Notes

- The client requires WebView2 Runtime to be installed
- All JSBridge methods must be marked `[ComVisible(true)]`
- UI operations in JSBridge must use `Dispatcher.Invoke`
- Frontend uses `await` when calling JSBridge methods
- JSBridge methods should return JSON strings with `success` and `message` fields
