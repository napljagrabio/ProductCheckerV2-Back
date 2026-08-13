namespace ProductCheckerBack.ExecutionState.DefaultStateHandler
{
    internal interface IHandler
    {
        IHandler NextHandler { get; set; }
        void Process(ArtemisDbContext productCheckerDbContext, ProductCheckerService productCheckerService, List<string> errors, bool onlyErrors = false);
    }
}