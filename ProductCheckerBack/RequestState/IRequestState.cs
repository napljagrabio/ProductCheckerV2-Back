namespace ProductCheckerBack.ProductCheckerState
{
    internal interface IRequestState
    {
        void Process(ProductCheckerService productCheckerService);
    }
}