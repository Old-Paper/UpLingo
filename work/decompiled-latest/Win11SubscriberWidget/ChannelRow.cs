using System.Windows.Forms;

namespace Win11SubscriberWidget;

internal class ChannelRow
{
	public int ChannelIndex;

	public CardPanel Card;

	public CountDisplay CountLabel;

	public Label DetailLabel;

	public Label MilestoneLabel;

	public MilestoneBar MilestoneProgressBar;

	public DeltaBar ProgressBar;
}
