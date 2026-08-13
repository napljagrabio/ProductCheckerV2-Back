namespace ProductCheckerBack.ProductCheckerState
{
    internal interface IExecutionState
    {
        void Process(ProductCheckerService productCheckerService);
    }
}