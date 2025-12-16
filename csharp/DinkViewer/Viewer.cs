namespace DinkViewer;

using DinkTool;
using Dink;
using System.Text;

public class ViewerSettings
{
    public string DestFolder {get; set;} = "";
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

        BrowserUtils.OpenURL(destFile);

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
    <h1>Dink Viewer: {_env.RootFilename}</h1>
    <div class=""search-container"">
        <input type=""text"" id=""lineIdInput"" placeholder=""Enter LineID..."">
        <button id=""searchButton"">Find</button>
    </div>
    <div id=""dink-root""></div>

    <script id=""dink-data"" type=""application/json"">{jsonContent}</script>
    <script>{GetJavascript()}</script>
</body>
</html>
";
    }

    private static string GetCss()
    {
        return @"
            body { font-family: 'Segoe UI', sans-serif; padding: 20px; color: #333; }
            h1 { border-bottom: 2px solid #eee; padding-bottom: 10px; }
            .search-container { margin-bottom: 20px; }
            #lineIdInput { padding: 8px; width: 300px; border: 1px solid #ccc; border-radius: 4px; }
            #searchButton { padding: 8px 12px; border: none; background-color: #007bff; color: white; border-radius: 4px; cursor: pointer; }
            #searchButton:hover { background-color: #0056b3; }
            div.dink-indent { margin-left: 20px; border-left: 1px solid #eee; padding-left: 20px; }
            details > summary { cursor: pointer; font-weight: bold; padding: 5px; background: #f0f0f0; border-radius: 4px; margin-bottom: 5px; }
            details > summary:hover { background: #e0e0e0; }
            .comments { font-style: italic; color: #666; margin-left: 10px; display:inline; }
            .beat { border-left: 3px solid #ddd; padding-left: 15px; margin: 10px 0; }
            .beat-content { display: grid; grid-template-columns: 120px 1fr; gap: 10px; }
            .beat .character { font-weight: bold; text-align: right; }
            .beat .text { white-space: pre-wrap; }
        ";
    }

    private static string GetJavascript()
    {
        return @"
document.addEventListener('DOMContentLoaded', () => {
    const jsonText = document.getElementById('dink-data').textContent;
    const scenes = JSON.parse(jsonText);
    const rootElement = document.getElementById('dink-root');

    function createCommentElement(comments) {
        if (!comments || comments.length === 0) return null;
        const container = document.createElement('div');
        container.className = 'comments';
        container.textContent = `// ${comments.join(', ')}`;
        return container;
    }

    function createBeatElement(beat) {
        const beatDiv = document.createElement('div');
        beatDiv.className = 'beat';
        if (beat.LineID) {
            beatDiv.dataset.lineid = beat.LineID;
        }

        const content = document.createElement('div');
        content.className = 'beat-content';

        if (beat.hasOwnProperty('CharacterID')) { // It's a line
            const char = document.createElement('div');
            char.className = 'character';
            char.textContent = beat.CharacterID + (beat.Qualifier ? ` (${beat.Qualifier})` : '');
            content.appendChild(char);

            const text = document.createElement('div');
            text.className = 'text';
            text.textContent = beat.Text;
            content.appendChild(text);
        } else { // It's an action
            const char = document.createElement('div');
            char.className = 'character';
            char.textContent = 'ACTION';
            content.appendChild(char);

            const text = document.createElement('div');
            text.className = 'text';
            text.textContent = beat.Text;
            content.appendChild(text);
        }
        
        beatDiv.appendChild(content);

        const comments = createCommentElement(beat.Comments);
        if (comments) beatDiv.appendChild(comments);
        
        return beatDiv;
    }

    function createSnippetElement(snippet) {
        const details = document.createElement('details');
        details.className = 'snippet';
        details.open = true;

        const summary = document.createElement('summary');
        summary.textContent = `Snippet: ${snippet.SnippetID}`;
        if (snippet.Group > 0) {
            summary.textContent += ` (Group ${snippet.GroupIndex}/${snippet.GroupCount})`;
        }
        const comments = createCommentElement(snippet.Comments);
        if (comments) summary.appendChild(comments);
        
        details.appendChild(summary);

        const content = document.createElement('div');
        content.className = 'dink-indent';
        snippet.Beats.forEach(beat => content.appendChild(createBeatElement(beat)));
        details.appendChild(content);

        return details;
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
        summary.textContent = `Group (${first.GroupCount})`;
        const comments = createCommentElement(first.GroupComments);
        if (comments) summary.appendChild(comments);

        details.appendChild(summary);

        const content = document.createElement('div');
        content.className = 'dink-indent';
        snippets.forEach(snippet => content.appendChild(createSnippetElement(snippet)));
        details.appendChild(content);

        return details;
    }

    function createBlockElement(block) {
        const details = document.createElement('details');
        details.className = 'block';

        const summary = document.createElement('summary');
        summary.textContent = `Block: ${block.BlockID || '(main)'}`;
        const comments = createCommentElement(block.Comments);
        if (comments) summary.appendChild(comments);
        
        details.appendChild(summary);

        const content = document.createElement('div');
        content.className = 'dink-indent';
        details.appendChild(content);

        const groupedSnippets = {};
        block.Snippets.forEach(snippet => {
            // Use SnippetID for non-grouped snippets to give them a unique group
            const groupId = snippet.Group > 0 ? snippet.Group : snippet.SnippetID;
            if (!groupedSnippets[groupId]) {
                groupedSnippets[groupId] = [];
            }
            groupedSnippets[groupId].push(snippet);
        });
        
        Object.values(groupedSnippets).forEach(group => {
            content.appendChild(createSnippetGroupElement(group));
        });

        return details;
    }

    function createSceneElement(scene) {
        const details = document.createElement('details');
        details.className = 'scene';

        const summary = document.createElement('summary');
        summary.textContent = `Scene: ${scene.SceneID}`;
        const comments = createCommentElement(scene.Comments);
        if (comments) summary.appendChild(comments);
        
        details.appendChild(summary);
        
        const content = document.createElement('div');
        content.className = 'dink-indent';
        scene.Blocks.forEach(block => content.appendChild(createBlockElement(block)));
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
});
";
    }
}
