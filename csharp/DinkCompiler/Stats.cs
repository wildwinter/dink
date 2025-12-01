namespace DinkCompiler;
using Dink;

class Stats
{
    public static bool WriteExcelFile(List<DinkScene> dinkScenes, 
            List<NonDinkLine> nonDinkLines, 
            LocStrings inkStrings, VoiceLines voiceLines, 
            WritingStatuses writingStatuses, 
            string destStatsFile)
    {
        // -- Overall

        // Total Words

        // Total Lines
        // Lines at Each Writing Status

        // Total Dialogue Lines
        // Lines at Each Recording Status

        // -- Scenes
        
        // Scene Writing State
        // Scene Recording State

        // -- Actors
        // Lines Recorded Per Character / Actor
        // Lines To Be Recorded Per Character / Actor

        // -- Line Status
        // Each line, writing status, recording status

        return true;
    }
}