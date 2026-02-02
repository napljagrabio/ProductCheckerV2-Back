namespace ProductCheckerBack.RequestState.DefaultStateHandler
{
    internal interface IHandler
    {
        IHandler NextHandler { get; set; }
        void Process(ProductCheckerDbContext productCheckerDbContext, ProductCheckerService productCheckerService, List<string> errors, bool onlyErrors = false);
    }
}