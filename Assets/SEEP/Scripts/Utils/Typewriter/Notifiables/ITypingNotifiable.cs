namespace SEEP.Utils.Typewriter.Notifiables
{
    public interface ITypingNotifiable
    {
        void OnTypingBegin();
        void OnCaretMove();
        void OnTypingEnd();
    }
}