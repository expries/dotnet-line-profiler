using SharpPdb.Managed;

namespace PdbReader.DebugInfo
{
    public record LineInfo
    {
        public int StartLine;
        public int EndLine;
        public int StartColumn;
        public int EndColumn;
    }
    
    public class Pdb
    {
        private readonly IPdbFile _pdb;
        
        public Pdb(string pdbPath)
        {
            _pdb = PdbFileReader.OpenPdb(pdbPath);
        }
        
        public void Print()
        {
            var dictionary = GetLineInfoByAssemblyAndInstructionOffset();
            foreach (var (assembly, lineInfoByInstructionOffset) in dictionary)
            {
                Console.WriteLine($"Assembly: {assembly}");
                foreach (var (instructionOffset, lineInfo) in lineInfoByInstructionOffset)
                {
                    foreach (var line in lineInfo)
                    {
                        Console.WriteLine($"[{instructionOffset}] => Lines {line.StartLine} - {line.EndLine} Columns ({line.StartColumn} - {line.EndColumn})");
                    }
                }
            }
        }

        public Dictionary<string, Dictionary<int, List<LineInfo>>> GetLineInfoByAssemblyAndInstructionOffset()
        {
            var result = new Dictionary<string, Dictionary<int, List<LineInfo>>>();
            var startOffset = 0;
            
            var sequencePoints = _pdb.Functions.SelectMany(f => f.SequencePoints);

            foreach (var sequencePoint in sequencePoints)
            {
                var sourceName = sequencePoint.Source.Name;

                if (!result.ContainsKey(sourceName))
                {
                    result[sourceName] = new Dictionary<int, List<LineInfo>>();
                }
                
                var absoluteOffset = sequencePoint.Offset;

                if (!result[sourceName].ContainsKey(absoluteOffset))
                {
                    result[sourceName][absoluteOffset] = new List<LineInfo>();
                }

                result[sourceName][absoluteOffset].Add(new LineInfo
                {
                    StartLine = sequencePoint.StartLine, EndLine = sequencePoint.EndLine,
                    StartColumn = sequencePoint.StartColumn, EndColumn = sequencePoint.EndColumn
                });

                startOffset += sequencePoint.Offset;
            }

            return result;
        }
    }
}