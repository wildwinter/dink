using DinkCompiler;

var options = new Compiler.Options();

// ----- Simple Args -----
foreach (var arg in args)
{
    if (arg.StartsWith("--source="))
        options.source = arg.Substring(9);
    else if (arg.StartsWith("--destFolder="))
        options.destFolder = arg.Substring(14);
}

// ----- Parse Ink, Update Tags, Build String List -----
var compiler = new Compiler(options);
if (!compiler.Run()) {
    Console.Error.WriteLine("Not compiled.");
    return -1;
}

return 0;