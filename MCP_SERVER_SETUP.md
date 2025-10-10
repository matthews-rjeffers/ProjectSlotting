# MCP Server Setup Guide

## ✅ Installed MCP Servers

I've successfully installed the following MCP servers:

1. **@modelcontextprotocol/server-sequential-thinking** - For complex problem solving
2. **@modelcontextprotocol/server-filesystem** - For enhanced file system access
3. **puppeteer-mcp-server** - For browser automation and testing
4. **@modelcontextprotocol/server-memory** - For persistent knowledge across sessions
5. **@modelcontextprotocol/server-fetch** - For HTTP requests and API testing
6. **@modelcontextprotocol/server-github** - For GitHub integration (requires token)
7. **@modelcontextprotocol/server-git** - For Git operations and version control

## Configuration for Claude Code (VS Code Extension)

Since you're using Claude Code in VS Code, the MCP configuration is done in VS Code settings.

### Step 1: Open VS Code Settings

1. Press `Ctrl+Shift+P` to open Command Palette
2. Type "Preferences: Open User Settings (JSON)"
3. Press Enter

### Step 2: Add MCP Configuration

Add this to your `settings.json`:

```json
{
  "claude.mcpServers": {
    "sequential-thinking": {
      "command": "npx",
      "args": [
        "-y",
        "@modelcontextprotocol/server-sequential-thinking"
      ]
    },
    "filesystem": {
      "command": "npx",
      "args": [
        "-y",
        "@modelcontextprotocol/server-filesystem",
        "c:\\Learning\\DEMO"
      ]
    },
    "puppeteer": {
      "command": "npx",
      "args": [
        "-y",
        "puppeteer-mcp-server"
      ]
    },
    "memory": {
      "command": "npx",
      "args": [
        "-y",
        "@modelcontextprotocol/server-memory"
      ]
    },
    "fetch": {
      "command": "npx",
      "args": [
        "-y",
        "@modelcontextprotocol/server-fetch"
      ]
    },
    "github": {
      "command": "npx",
      "args": [
        "-y",
        "@modelcontextprotocol/server-github"
      ],
      "env": {
        "GITHUB_PERSONAL_ACCESS_TOKEN": "your_github_personal_access_token_here"
      }
    },
    "git": {
      "command": "npx",
      "args": [
        "-y",
        "@modelcontextprotocol/server-git",
        "--repository",
        "c:\\Learning\\DEMO"
      ]
    }
  }
}
```

### Step 3: Restart VS Code

After adding the configuration, restart VS Code completely to activate the MCP servers.

## How to Use MCP Servers

Once configured and VS Code is restarted, I will automatically have access to:

### Sequential Thinking
- Break down complex problems into steps
- Better architectural planning
- Multi-step debugging

### Filesystem
- More efficient file tree navigation
- Better directory operations
- Enhanced file search

### Puppeteer
- Automated UI testing
- Browser automation
- Screenshot capture
- Form filling and clicking

### Memory
- Store and recall project decisions
- Maintain context across sessions
- Remember architectural choices

### Fetch
- Test API endpoints
- Make HTTP requests
- Verify backend responses

### GitHub
- Create and manage issues
- Submit pull requests
- Search repositories
- **Note**: Requires GitHub Personal Access Token in settings

### Git
- Commit changes
- View history and diffs
- Manage branches
- Repository operations

## Verifying Installation

After adding the configuration:

1. **Restart VS Code** completely (close and reopen)
2. **Check for MCP tools**: I should see tools like `mcp__sequential_thinking`, `mcp__filesystem`, etc.
3. **Test a command**: Ask me to "use sequential thinking to analyze this problem"

## Recommended for Your Project

Based on your SQL Server + ASP.NET Core + React project, the most valuable MCP servers would be:

### High Priority:
1. **Sequential Thinking** ✅ (Installed) - Complex problem solving
2. **Filesystem** ✅ (Installed) - Better file navigation
3. **Puppeteer** ✅ (Installed) - UI testing automation
4. **Memory** ✅ (Installed) - Remember decisions across sessions
5. **Fetch** ✅ (Installed) - HTTP requests and API testing
6. **GitHub** ✅ (Installed) - GitHub integration for repos, PRs, issues
7. **Git** ✅ (Installed) - Version control operations

## Alternative: Using Claude Desktop

If you're using Claude Desktop instead of Claude Code:

1. Find config file:
   - Windows: `%APPDATA%\Claude\claude_desktop_config.json`
   - Mac: `~/Library/Application Support/Claude/claude_desktop_config.json`

2. Add MCP servers:
```json
{
  "mcpServers": {
    "sequential-thinking": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-sequential-thinking"]
    },
    "filesystem": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-filesystem", "c:\\Learning\\DEMO"]
    },
    "puppeteer": {
      "command": "npx",
      "args": ["-y", "puppeteer-mcp-server"]
    }
  }
}
```

3. Restart Claude Desktop

## Testing MCP Servers

Once configured, you can test by asking me:

- "Use sequential thinking to plan the next feature"
- "Use puppeteer to test the squad creation form"
- "Search the filesystem for all .jsx files"

## Troubleshooting

### MCP servers not showing up?
1. Restart VS Code completely
2. Check the Output panel for "Claude" logs
3. Verify NPM packages are installed globally: `npm list -g --depth=0`

### Permission errors?
Run VS Code as Administrator on Windows

### NPX not found?
Make sure Node.js is in your PATH and restart VS Code

## Benefits for This Project

With these MCP servers installed, I can:

1. **Sequential Thinking**: Better plan complex features like multi-squad allocation algorithms
2. **Filesystem**: Navigate your React components and C# files more efficiently
3. **Puppeteer**: Automatically test the UI flows (create squad → add members → allocate project)
4. **Memory**: Remember your project decisions and architectural patterns across sessions
5. **Fetch**: Test your ASP.NET Core API endpoints directly
6. **GitHub**: Help manage issues, PRs, and repository workflows
7. **Git**: Manage commits, branches, and version control operations

## Next Steps

1. ✅ MCP servers are configured in VS Code settings
2. ⏳ Add GitHub Personal Access Token to settings (if using GitHub server)
3. ⏳ Restart VS Code to activate new servers
4. ✅ Start using MCP-enhanced features!

### To Add GitHub Token:
1. Go to https://github.com/settings/tokens
2. Generate a new token with repo permissions
3. Replace `<your_github_token_here>` in settings.json
4. Restart VS Code

Let me know if you need help with any of these steps!
