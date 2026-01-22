using ProductCheckerBack.ProductCheckerState;
using ProductCheckerBack.RequestState.SuccessStateHandler;

namespace ProductCheckerBack.RequestState
{
    internal class SuccessState : IRequestState
    {
        private readonly ProductCheckerV2DbContext _productCheckerV2DbContext;

        public SuccessState(ProductCheckerV2DbContext productCheckerV2DbContext)
        {
            _productCheckerV2DbContext = productCheckerV2DbContext;
        }

        public void Process(ProductCheckerService productCheckerService)
        {
            productCheckerService.MarkAsProcessing();

            HandlerProcessor successHandlers = new HandlerProcessor()
            {
                typeof(CheckProductAvailability)
            };
            successHandlers.Process(_productCheckerV2DbContext, productCheckerService, new List<string>());
        }
    }
}