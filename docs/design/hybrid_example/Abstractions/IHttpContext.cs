namespace HybridExample
{
    public interface IHttpContext
    {
        IRequest Request { get; }
        IResponse Response { get; }
    }
}
