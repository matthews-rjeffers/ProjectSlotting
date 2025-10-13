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

**IMPORTANT**: Claude Code uses a `.mcp.json` file in your project root, NOT VS Code settings.json!

### Step 1: Create `.mcp.json` File

Create a file named `.mcp.json` in your project root (`c:\Learning\DEMO\.mcp.json`):

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
    },
    "memory": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-memory"]
    },
    "fetch": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-fetch"]
    },
    "github": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-github"],
      "env": {
        "GITHUB_PERSONAL_ACCESS_TOKEN": "your_token_here"
      }
    },
    "git": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-git", "--repository", "c:\\Learning\\DEMO"]
    }
  }
}
```

**Note**: The `.mcp.json` file has already been created in this project!

### Step 2: Restart VS Code

After creating the `.mcp.json` file, restart VS Code completely to activate the MCP servers.

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
1. **Check for `.mcp.json` file** in project root (NOT settings.json!)
2. Restart VS Code completely (close all windows, not just reload)
3. Check the Output panel (Ctrl+Shift+U) → Select "Claude Code" from dropdown
4. Look for messages like "Starting MCP server: sequential-thinking"
5. Verify NPM/Node.js is working: `npx --version`

### Common Issues:
- **Wrong configuration location**: MCP config must be in `.mcp.json`, not `settings.json`
- **No restart**: Must completely close and reopen VS Code after creating `.mcp.json`
- **Path issues**: Ensure Node.js/NPM is in your PATH

### Permission errors?
Run VS Code as Administrator on Windows

### Checking Output Logs:
1. Press `Ctrl+Shift+U` to open Output panel
2. Select "Claude Code" from the dropdown (top-right of Output panel)
3. Look for MCP server startup messages or errors

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

1. ✅ `.mcp.json` file created in project root
2. ⏳ Add GitHub Personal Access Token to `.mcp.json` (if using GitHub server)
3. ⏳ Restart VS Code to activate new servers
4. ⏳ Verify in Output logs (Ctrl+Shift+U → "Claude Code")
5. ✅ Start using MCP-enhanced features!

### To Add GitHub Token:
1. Go to https://github.com/settings/tokens
2. Generate a new token with repo permissions
3. Replace `"your_token_here"` in `.mcp.json` (line 50)
4. Restart VS Code

Let me know if you need help with any of these steps!
