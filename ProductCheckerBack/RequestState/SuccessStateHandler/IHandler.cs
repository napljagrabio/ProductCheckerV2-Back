namespace ProductCheckerBack.RequestState.SuccessStateHandler
{
    internal interface IHandler
    {
        IHandler NextHandler { get; set; }
        void Process(ProductCheckerV2DbContext productCheckerV2DbContext, ProductCheckerService productCheckerService, List<string> errors, bool onlyErrors = false);
    }
}