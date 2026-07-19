using System.Collections.Generic;

namespace Win11SubscriberWidget;

internal sealed class RefreshPayload
{
	public List<FetchResult> Results;

	public CreatorFetchData Creator;
}
