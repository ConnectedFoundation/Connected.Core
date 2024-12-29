using Connected.Annotations;

namespace Connected.Services;

[Service]
public interface ICancellationContext
{
	CancellationToken CancellationToken { get; }
	void Cancel();
}