namespace ProductCheckerBack.RequestState.SuccessStateHandler
{
    internal class HandlerProcessor : List<Type>
    {
        private IHandler InitializeHandlers()
        {
            IHandler handlerChain = null;
            ForEach(handlerType => {
                var handlerInstance = Activator.CreateInstance(handlerType) as IHandler;
                if (handlerChain == null)
                {
                    handlerChain = handlerInstance;
                }
                else
                {
                    ChainHandler(handlerChain, handlerInstance);
                }
            });

            return handlerChain;
        }

        private void ChainHandler(IHandler chain, IHandler newHandler)
        {
            IHandler currentHandler = chain;
            while (currentHandler.NextHandler != null)
            {
                currentHandler = currentHandler.NextHandler;
            }

            currentHandler.NextHandler = newHandler;
        }

        public void Process(ProductCheckerDbContext productCheckerDbContext, ProductCheckerService productCheckerService, List<string> errors, bool onlyErrors = false)
        {
            IHandler handler = InitializeHandlers();
            handler.Process(productCheckerDbContext, productCheckerService, errors, onlyErrors);
        }
    }
}