namespace DinkCompiler;

class LiveBuilder
{
    private Compiler _compiler;
    private System.Timers.Timer _timer;
    
    // Store specific file paths we care about
    private HashSet<string> _watchedFiles = new HashSet<string>();
    
    // Track directories we are already watching to avoid duplicate watchers
    private HashSet<string> _watchedDirectories = new HashSet<string>();
    
    private List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();
    private object _lock = new object();
    private bool _isCompiling = false;

    public LiveBuilder(Compiler compiler)
    {
        _compiler = compiler;
        // 500ms debounce
        _timer = new System.Timers.Timer(1000); 
        _timer.AutoReset = false;
        _timer.Elapsed += OnTimerElapsed;
    }

    public int Run()
    {
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine("DINK LIVE BUILDER");
        Console.WriteLine("Performing initial build...");
        Console.WriteLine("--------------------------------------------------");

        TriggerBuild();

        Console.WriteLine("Press 'q' and Enter to quit.");
        while (true)
        {
            var key = Console.ReadLine();
            if (key?.Trim().ToLower() == "q") break;
        }
        
        StopWatchers();
        return 0;
    }

    private void TriggerBuild()
    {
        _timer.Stop();
        OnTimerElapsed(null, null);
    }
    
    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        bool isWatched = false;
        lock (_lock)
        {
            isWatched = _watchedFiles.Contains(Path.GetFullPath(e.FullPath));
        }

        if (!isWatched)
            return;

        _timer.Stop();
        _timer.Start();
    }
    
    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        bool isRelevant = false;
        lock (_lock)
        {
            isRelevant = _watchedFiles.Contains(Path.GetFullPath(e.OldFullPath)) || 
                         _watchedFiles.Contains(Path.GetFullPath(e.FullPath));
        }

        if (!isRelevant)
            return;

        _timer.Stop();
        _timer.Start();
    }

    private void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs? e)
    {
        lock (_lock)
        {
             if (_isCompiling) return;
             _isCompiling = true;
        }

        Console.WriteLine($"\n[Live] Build started at {DateTime.Now.ToLongTimeString()}...");
        
        // Suspend watchers so the compiler's own file writes don't trigger new builds
        SuspendWatchers();
        
        bool success = false;
        try 
        {
            success = _compiler.Run();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[Live] Compiler crashed: " + ex.Message);
        }

        UpdateWatchers();
        ResumeWatchers();

        if (success)
            Console.WriteLine("[Live] Build Succeeded. Watching for changes...");
        else
            Console.WriteLine("[Live] Build Failed. Watching for fixes...");

        lock (_lock)
        {
            _isCompiling = false;
        }
    }

    private void SuspendWatchers()
    {
        foreach(var w in _watchers)
        {
            w.EnableRaisingEvents = false;
        }
    }

    private void ResumeWatchers()
    {
        foreach(var w in _watchers)
        {
            w.EnableRaisingEvents = true;
        }
    }

    private void UpdateWatchers()
    {
        var newWatchedFiles = _compiler.UsedInkFiles
            .Select(f => Path.GetFullPath(f))
            .ToHashSet();

        lock (_lock)
        {
            _watchedFiles = newWatchedFiles;
        }

        var requiredDirectories = newWatchedFiles
            .Select(f => Path.GetDirectoryName(f))
            .Where(d => !string.IsNullOrEmpty(d))
            .Distinct()
            .ToHashSet();

        if (_watchedDirectories.SetEquals(requiredDirectories!))
            return;

        StopWatchers();
        
        _watchedDirectories = requiredDirectories!;
        
        foreach (var dir in _watchedDirectories)
        {
             if (Directory.Exists(dir))
             {
                 var watcher = new FileSystemWatcher(dir, "*.ink");
                 watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime;
                 watcher.Changed += OnFileChanged;
                 watcher.Created += OnFileChanged;
                 watcher.Deleted += OnFileChanged;
                 watcher.Renamed += OnRenamed;
                 
                 // We create them enabled, but they might be effectively suspended if we are inside OnTimerElapsed.
                 // ResumeWatchers() will ensure they are on before we exit the build loop.
                 watcher.EnableRaisingEvents = true; 
                 _watchers.Add(watcher);
                 Console.WriteLine($"[Live] Watching directory: {dir}");
             }
        }
    }

    private void StopWatchers()
    {
        foreach(var w in _watchers) { 
            w.EnableRaisingEvents = false; 
            w.Dispose(); 
        }
        _watchers.Clear();
    }
}