using DinkCompiler;
using System.CommandLine;

var options = new Compiler.Options();

RootCommand command = new("Compiler chain for Dink");

Option<FileInfo> sourceOption = new("--source")
{
    Description = "The root Ink file to use as the source for the compile.",
    Required = true
};
command.Options.Add(sourceOption);

Option<FileInfo> destFolderOption = new("--destFolder")
{
    Description = "The destination folder to write out all the compiled files.",
    DefaultValueFactory = parseResult => new FileInfo(Environment.CurrentDirectory)
};
command.Options.Add(destFolderOption);

command.SetAction(parseResult =>
{
    options.source = parseResult.GetValue<FileInfo>(sourceOption)?.FullName ?? "";
    options.destFolder = parseResult.GetValue<FileInfo>(destFolderOption)?.FullName ?? "";

    var compiler = new Compiler(options);
    if (!compiler.Run()) {
        Console.Error.WriteLine("Not compiled.");
        return -1;
    }
    return 0;
});

ParseResult parseResult = command.Parse(args);
return parseResult.Invoke();