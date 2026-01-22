using ProductCheckerBack.ProductCheckerState;
using ProductCheckerBack.RequestState.SuccessStateHandler;

namespace ProductCheckerBack.RequestState
{
    internal class ProcessingState : IRequestState
    {
        private readonly ProductCheckerV2DbContext _productCheckerV2DbContext;

        public ProcessingState(ProductCheckerV2DbContext productCheckerV2DbContext)
        {
            _productCheckerV2DbContext = productCheckerV2DbContext;
        }

        public void Process(ProductCheckerService productCheckerService)
        {
            productCheckerService.MarkAsProcessing();

            HandlerProcessor processingHandlers = new HandlerProcessor()
            {
                typeof(CheckProductAvailability)
            };
            processingHandlers.Process(_productCheckerV2DbContext, productCheckerService, new List<string>());
        }
    }
}