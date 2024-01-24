namespace SEEP.Utils.Typewriter.Parsing
{
    internal class SpriteNode : INode
    {
        public string Value { get; }

        public SpriteNode(string value)
        {
            Value = value;
        }
    }
}