namespace DinkViewer;

using DinkTool;
using Dink;
using System.Text;

public class ViewerSettings
{
    public string DestFolder {get; set;} = "";
    public bool Silent { get; set; } = false;
    public bool Export {get; private set;} = true;

    public bool Init()
    {
        if (string.IsNullOrEmpty(DestFolder))
        {
            string systemTempPath = Path.GetTempPath();
            string randomName = Path.GetRandomFileName();
            string tempDirectoryPath = Path.Combine(systemTempPath, randomName);
            DestFolder = tempDirectoryPath;
            Export = false;
        }
        if (!Path.IsPathFullyQualified(DestFolder))
        {
            DestFolder = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, DestFolder));
        }
        return true;
    }
}

public class Viewer
{
    private ProjectEnvironment _env;
    private ViewerSettings _viewerSettings;

    public Viewer(ProjectEnvironment env, ViewerSettings exportSettings)
    {
        _env = env;
        _viewerSettings = exportSettings;

    }

    public bool Run()
    {
        if (!ReadStructureJson(_env.DestDinkStructureFile, out string jsonContent))
            return false;

        Directory.CreateDirectory(_viewerSettings.DestFolder);
        string destFile = Path.Combine(_viewerSettings.DestFolder, _env.RootFilename+"-viewer.html");
        
        string html = GenerateViewDoc(jsonContent);
        File.WriteAllText(destFile, html, Encoding.UTF8);
    
        Console.WriteLine($"Wrote {destFile}");
    
        if (!_viewerSettings.Silent)
        {
            Console.WriteLine($"Opening in browser...");
            BrowserUtils.OpenURL(destFile);
        }

        return true;
    }

    private bool ReadStructureJson(string scenesFile, out string jsonContent)
    {
        jsonContent = "";
        if (!File.Exists(scenesFile))
        {
            Console.WriteLine($"{scenesFile} not found - make sure the Dink Compiler was run using --outputStructure before using this utility.");
            return false;
        }

        jsonContent = File.ReadAllText(scenesFile);
        Console.WriteLine($"Read {scenesFile}.");
        return true;
    }
    
    private string GenerateViewDoc(string jsonContent)
    { 
        return $@"
<!DOCTYPE html>
<html>
<head>
    <title>Dink Viewer: {_env.RootFilename}</title>
    <style>{GetCss()}</style>
</head>
<body>
    <header class=""main-header"">
        <h1>Dink Viewer: {_env.RootFilename}</h1>
        <div class=""search-container"">
            <input type=""text"" id=""lineIdInput"" placeholder=""Enter LineID..."">
            <button id=""searchButton"">Find ID</button>
            <div class=""view-controls"">
                <button id=""expandAllButton"" title=""Expand All"">▼</button>
                <button id=""collapseAllButton"" title=""Collapse All"">▶</button>
                <button id=""printButton"" title=""Print"">🖨️</button>
            </div>
        </div>
    </header>
    <main class=""main-content"">
        <div id=""dink-root""></div>
    </main>

    <script id=""dink-data"" type=""application/json"">{jsonContent}</script>
    <script>{GetJavascript(_env.LocActions)}</script>
</body>
</html>
";
    }

    private static string GetCss()
    {
        return @"
            html {
                height: 100%;
                overflow: hidden;
            }
            body { 
                height: 100%;
                margin: 0;
                font-family: 'Segoe UI', sans-serif; 
                color: #333; 
                display: flex;
                flex-direction: column;
            }
            .main-header {
                padding: 20px;
                border-bottom: 2px solid #eee;
            }
            .main-content {
                flex-grow: 1;
                overflow-y: auto;
                padding: 20px;
            }
            h1 { 
                border-bottom: none;
                padding-bottom: 10px; 
                margin: 0 0 10px 0;
            }
            .search-container { 
                display: flex; 
                align-items: center; 
            }
            .view-controls { margin-left: auto; }
            .view-controls button {
                padding: 4px 8px;
                border: 1px solid #ccc;
                background-color: #f0f0f0;
                border-radius: 4px;
                cursor: pointer;
                font-size: 1rem;
                margin-left: 5px;
            }
            .view-controls button:hover {
                background-color: #e0e0e0;
            }
            #lineIdInput { padding: 8px; width: 300px; border: 1px solid #ccc; border-radius: 4px; }
            #searchButton { padding: 8px 12px; border: none; background-color: #007bff; color: white; border-radius: 4px; cursor: pointer; }
            #searchButton:hover { background-color: #0056b3; }
            div.dink-indent { margin-left: 20px; border-left: 1px solid #eee; padding-left: 20px; }
            details > summary { cursor: pointer; font-weight: bold; padding: 5px; background: #f0f0f0; border-radius: 4px; margin-bottom: 5px; }
            details > summary:hover { background: #e0e0e0; }
            .header-comments { margin-left: 25px; margin-bottom: 5px; font-style: italic; color: #666; }
            .dink-indent > .comments { display: block; margin-left: 0; margin-bottom: 5px; }
            summary > .comments { display: inline-block; vertical-align: middle; }
            .comments { font-style: italic; color: #666; margin-left: 10px; display:inline; }
            .snippet { background: #f9f9f9; border-radius: 4px; padding: 10px; margin-bottom: 5px; }
            .snippet:hover { background: #ffffff; }
            .snippet > .comments { display: block; margin-left: 0; margin-bottom: 10px; }
            .beat {
                font-family: 'Courier New', Courier, monospace;
                border-left: 3px solid #ddd;
                padding-left: 15px;
                margin: 10px 0;
            }
            .beat > .comments { display: block; margin-left: 0; margin-bottom: 5px; }
            .beat .character {
                font-weight: normal;
                text-align: left;
                text-transform: uppercase;
                margin-left: 240px;
            }
            .beat .direction {
                margin-left: 180px;
            }
            .beat .text {
                display: flex;
                justify-content: space-between;
                align-items: flex-start;
                white-space: pre-wrap;
                margin-left: 120px;
            }
            .action-beat .text {
                margin-left: 0;
            }
            .identifier {
                flex-shrink: 0;
                margin-left: 20px;
                color: grey;
                font-size: 0.8em;
                cursor: pointer;
                font-family: 'Segoe UI', sans-serif;
                opacity: 0.5;
                transition: opacity 0.2s;
            }
            .identifier:hover {
                color: black;
                opacity: 1;
            }

            @media print {
                html, body {
                    height: auto;
                    overflow: visible;
                }
                body {
                    display: block;
                }
                .main-header {
                    display: none;
                }
                .main-content {
                    overflow: visible;
                    padding: 0;
                }
                details > summary::-webkit-details-marker {
                    display: none;
                }
                details > summary {
                    list-style: none;
                }
                .snippet, .beat, details {
                    page-break-inside: avoid;
                }
                [data-lineid] {
                    background-color: transparent !important;
                }
            }
        ";
    }

    private static string GetJavascript(bool locActions)
    {
        var settings = $"const dinkConfig = {{ locActions: {locActions.ToString().ToLower()} }};\n";
        return settings+@"
    document.addEventListener('DOMContentLoaded', () => {
    const jsonText = document.getElementById('dink-data').textContent;
    const scenes = JSON.parse(jsonText);
    const rootElement = document.getElementById('dink-root');

    function createCommentElement(comments) {
        if (!comments || comments.length === 0) return null;
        const container = document.createElement('div');
        container.className = 'comments';
        container.innerHTML = comments.map(c => `// ${c}`).join('<br>');
        return container;
    }

    function createIdElement(id) {
        const idElement = document.createElement('span');
        idElement.className = 'identifier';
        idElement.textContent = id;
        idElement.title = 'Copy ID';

        idElement.addEventListener('click', (e) => {
            e.preventDefault();
            navigator.clipboard.writeText(id).then(() => {
                const originalText = idElement.textContent;
                idElement.textContent = 'Copied!';
                setTimeout(() => {
                    idElement.textContent = originalText;
                }, 1000);
            }, (err) => {
                console.error('Could not copy text: ', err);
                alert('Could not copy ID.');
            });
        });
        return idElement;
    }

    function createBeatElement(beat) {
        const beatDiv = document.createElement('div');
        beatDiv.className = 'beat';
        if (beat.LineID) {
            beatDiv.dataset.lineid = beat.LineID;
        }
        if (beat.Origin) {
            beatDiv.title = `${beat.Origin.SourceFilePath}, line ${beat.Origin.LineNum}`;
        }

        const comments = createCommentElement(beat.Comments);
        if (comments) beatDiv.appendChild(comments);

        const isAction = !beat.hasOwnProperty('CharacterID');
        if (isAction) { // It's an action
            beatDiv.classList.add('action-beat');
            const text = document.createElement('div');
            text.className = 'text';
            
            const dialogueSpan = document.createElement('span');
            dialogueSpan.textContent = beat.Text;
            text.appendChild(dialogueSpan);

            if (beat.LineID && dinkConfig.locActions) {
                text.appendChild(createIdElement(beat.LineID));
            }
            beatDiv.appendChild(text);
        } else { // It's a line
            const char = document.createElement('div');
            char.className = 'character';
            char.textContent = beat.CharacterID + (beat.Qualifier ? ` (${beat.Qualifier})` : '');
            beatDiv.appendChild(char);

            if (beat.Direction) {
                const direction = document.createElement('div');
                direction.className = 'direction';
                direction.textContent = `(${beat.Direction})`;
                beatDiv.appendChild(direction);
            }
            
            const text = document.createElement('div');
            text.className = 'text';
            
            const dialogueSpan = document.createElement('span');
            dialogueSpan.textContent = beat.Text;
            text.appendChild(dialogueSpan);

            if (beat.LineID) {
                text.appendChild(createIdElement(beat.LineID));
            }
            beatDiv.appendChild(text);
        }
        
        return beatDiv;
    }

    function createSnippetElement(snippet) {
        const snippetDiv = document.createElement('div');
        snippetDiv.className = 'snippet';
        if (snippet.Origin) {
            snippetDiv.title = `${snippet.Origin.SourceFilePath}, line ${snippet.Origin.LineNum}`;
        }

        const comments = createCommentElement(snippet.Comments);
        if (comments) snippetDiv.appendChild(comments);
        
        snippet.Beats.forEach(beat => snippetDiv.appendChild(createBeatElement(beat)));

        return snippetDiv;
    }
    
    function createSnippetGroupElement(snippets) {
        if (snippets.length === 1 && snippets[0].Group === 0) {
            return createSnippetElement(snippets[0]);
        }

        const details = document.createElement('details');
        details.className = 'snippet-group';
        details.open = true;
        
        const summary = document.createElement('summary');
        const first = snippets[0];
        summary.textContent = `Group (${first.GroupCount} snippets)`;
        if (first.Origin) {
            summary.title = `${first.Origin.SourceFilePath}, line ${first.Origin.LineNum}`;
        }
        details.appendChild(summary);

        if (first.GroupComments && first.GroupComments.length > 0) {
            const commentsContainer = document.createElement('div');
            commentsContainer.className = 'header-comments';
            first.GroupComments.forEach(commentText => {
                const commentLine = document.createElement('div');
                commentLine.textContent = `// ${commentText}`;
                commentsContainer.appendChild(commentLine);
            });
            details.appendChild(commentsContainer);
        }

        const content = document.createElement('div');
        content.className = 'dink-indent';
        snippets.forEach(snippet => content.appendChild(createSnippetElement(snippet)));
        details.appendChild(content);

        return details;
    }

    function addSnippetElementsTo(snippets, parentElement) {
        const groupedSnippets = {};
        snippets.forEach(snippet => {
            // Use SnippetID for non-grouped snippets to give them a unique group
            const groupId = snippet.Group > 0 ? snippet.Group : snippet.SnippetID;
            if (!groupedSnippets[groupId]) {
                groupedSnippets[groupId] = [];
            }
            groupedSnippets[groupId].push(snippet);
        });
        
        Object.values(groupedSnippets).forEach(group => {
            parentElement.appendChild(createSnippetGroupElement(group));
        });
    }

    function createBlockElement(block) {
        const details = document.createElement('details');
        details.className = 'block';

        const summary = document.createElement('summary');
        summary.textContent = `Block: ${block.BlockID}`;
        if (block.Origin) {
            summary.title = `${block.Origin.SourceFilePath}, line ${block.Origin.LineNum}`;
        }
        details.appendChild(summary);

        if (block.Comments && block.Comments.length > 0) {
            const commentsContainer = document.createElement('div');
            commentsContainer.className = 'header-comments';
            block.Comments.forEach(commentText => {
                const commentLine = document.createElement('div');
                commentLine.textContent = `// ${commentText}`;
                commentsContainer.appendChild(commentLine);
            });
            details.appendChild(commentsContainer);
        }

        const content = document.createElement('div');
        content.className = 'dink-indent';
        details.appendChild(content);

        addSnippetElementsTo(block.Snippets, content);

        return details;
    }

    function createSceneElement(scene) {
        const details = document.createElement('details');
        details.className = 'scene';

        const summary = document.createElement('summary');
        summary.textContent = `Scene: ${scene.SceneID}`;
        if (scene.Origin) {
            summary.title = `${scene.Origin.SourceFilePath}, line ${scene.Origin.LineNum}`;
        }
        details.appendChild(summary);
        
        if (scene.Comments && scene.Comments.length > 0) {
            const commentsContainer = document.createElement('div');
            commentsContainer.className = 'header-comments';
            scene.Comments.forEach(commentText => {
                const commentLine = document.createElement('div');
                commentLine.textContent = `// ${commentText}`;
                commentsContainer.appendChild(commentLine);
            });
            details.appendChild(commentsContainer);
        }

        const content = document.createElement('div');
        content.className = 'dink-indent';

        scene.Blocks.forEach(block => {
            if (!block.BlockID) { // main block
                if (block.Comments && block.Comments.length > 0) {
                    block.Comments.forEach(commentText => {
                        const commentDiv = document.createElement('div');
                        commentDiv.className = 'comments';
                        commentDiv.textContent = `// ${commentText}`;
                        content.appendChild(commentDiv);
                    });
                }
                addSnippetElementsTo(block.Snippets, content);
            } else {
                content.appendChild(createBlockElement(block));
            }
        });
        
        details.appendChild(content);

        return details;
    }

    scenes.forEach(scene => rootElement.appendChild(createSceneElement(scene)));
    
    const searchButton = document.getElementById('searchButton');
    const lineIdInput = document.getElementById('lineIdInput');
    let lastHighlight = null;

    function search() {
        if (lastHighlight) {
            lastHighlight.style.backgroundColor = '';
        }

        const lineId = lineIdInput.value.trim();
        if (!lineId) return;

        const target = document.querySelector(`[data-lineid=""${lineId}""]`);
        if (target) {
            let parent = target.parentElement;
            while(parent) {
                if(parent.tagName === 'DETAILS') {
                    parent.open = true;
                }
                parent = parent.parentElement;
            }

            target.style.backgroundColor = '#fff3cd';
            target.scrollIntoView({ behavior: 'smooth', block: 'center' });
            
            lastHighlight = target;
        } else {
            alert('LineID not found.');
        }
    }

    searchButton.addEventListener('click', search);
    lineIdInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') {
            search();
        }
    });

    const expandAllButton = document.getElementById('expandAllButton');
    const collapseAllButton = document.getElementById('collapseAllButton');
    const printButton = document.getElementById('printButton');

    expandAllButton.addEventListener('click', () => {
        document.querySelectorAll('details').forEach(d => d.open = true);
    });

    collapseAllButton.addEventListener('click', () => {
        document.querySelectorAll('details').forEach(d => d.open = false);
    });

    printButton.addEventListener('click', () => {
        window.print();
    });

    let originalOpenState;

    window.addEventListener('beforeprint', () => {
        originalOpenState = new Map();
        document.querySelectorAll('details').forEach((d, i) => {
            originalOpenState.set(i, d.open);
            d.open = true;
        });
    });

    window.addEventListener('afterprint', () => {
        if (originalOpenState) {
            document.querySelectorAll('details').forEach((d, i) => {
                if (originalOpenState.has(i)) {
                    d.open = originalOpenState.get(i);
                }
            });
            originalOpenState = null;
        }
    });
});
";
    }
}