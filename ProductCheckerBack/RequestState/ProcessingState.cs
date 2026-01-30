using ProductCheckerBack.ProductCheckerState;
using ProductCheckerBack.RequestState.SuccessStateHandler;

namespace ProductCheckerBack.RequestState
{
    internal class ProcessingState : IRequestState
    {
        private readonly ProductCheckerDbContext _productCheckerDbContext;

        public ProcessingState(ProductCheckerDbContext productCheckerDbContext)
        {
            _productCheckerDbContext = productCheckerDbContext;
        }

        public void Process(ProductCheckerService productCheckerService)
        {
            productCheckerService.MarkAsProcessing();

            HandlerProcessor processingHandlers = new HandlerProcessor()
            {
                typeof(CheckProductAvailability)
            };
            processingHandlers.Process(_productCheckerDbContext, productCheckerService, new List<string>(), true);
        }
    }
}