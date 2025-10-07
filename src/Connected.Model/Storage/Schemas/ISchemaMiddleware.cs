namespace Connected.Storage.Schemas;

public interface ISchemaMiddleware : IMiddleware
{
	Task<bool> Invoke(ISchemaMiddlewareDto dto);
}
