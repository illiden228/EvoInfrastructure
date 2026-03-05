namespace _Project.Scripts.Application.Loading
{
    public readonly struct LoadingProgress
    {
        public readonly float Percent;
        public readonly string Message;
        public readonly int StepIndex;
        public readonly int StepCount;

        public LoadingProgress(float percent, string message, int stepIndex, int stepCount)
        {
            Percent = percent;
            Message = message;
            StepIndex = stepIndex;
            StepCount = stepCount;
        }
    }
}
