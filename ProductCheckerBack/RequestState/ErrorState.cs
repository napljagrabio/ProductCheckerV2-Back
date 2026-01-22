using ProductCheckerBack.Artemis;
using ProductCheckerBack.ProductCheckerState;

namespace ProductCheckerBack.RequestState
{
    internal class ErrorState : IRequestState
    {
        public void Process(ProductCheckerService productCheckerService)
        {
            productCheckerService.MarkAsFailed(["All Products is failed"]);
        }
    }
}