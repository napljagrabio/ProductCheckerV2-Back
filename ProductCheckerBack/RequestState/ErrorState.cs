using ProductCheckerBack.Artemis;
using ProductCheckerBack.ProductCheckerState;

namespace ProductCheckerBack.ExecutionState
{
    internal class ErrorState : IExecutionState
    {
        public void Process(ProductCheckerService productCheckerService)
        {
            productCheckerService.MarkAsFailed(["All Products is failed"]);
        }
    }
}