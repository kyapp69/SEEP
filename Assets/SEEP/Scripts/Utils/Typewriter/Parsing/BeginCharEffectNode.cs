using SEEP.Utils.Typewriter.Anims;

namespace SEEP.Utils.Typewriter.Parsing
{
    internal class BeginCharEffectNode : INode
    {
        public CharEffect CharEffect { get; }

        public BeginCharEffectNode(CharEffect charEffect)
        {
            CharEffect = charEffect;
        }
    }
}