using System;

using Android.Widget;

namespace EVI_ID.Mdl;

public sealed class OutputNavigator
{
private readonly TextView _output;
private readonly ScrollView _scrollView;

public OutputNavigator(
    TextView output,
    ScrollView scrollView)
{
    _output = output;
    _scrollView = scrollView;
}

public void Head()
{
    _scrollView.Post(() =>
    {
        _scrollView.ScrollTo(0, 0);
    });
}

public void Tail()
{
    _scrollView.Post(() =>
    {
        _scrollView.FullScroll(
            Android.Views.FocusSearchDirection.Down);
    });
}

public bool Goto(int line)
{
    if (line < 1)
        return false;

    _scrollView.Post(() =>
    {
        if (_output.Layout is null)
            return;

        int lineIndex =
            Math.Min(
                line - 1,
                _output.Layout.LineCount - 1);

        if (lineIndex < 0)
            return;

        int y =
            _output.Layout.GetLineTop(
                lineIndex);

        _scrollView.ScrollTo(
            0,
            y);
    });

    return true;
}

public void PageUp()
{
    _scrollView.Post(() =>
    {
        int amount =
            _scrollView.Height;

        if (amount <= 0)
            amount = 1;

        _scrollView.ScrollBy(
            0,
            -amount);
    });
}

public void PageDown()
{
    _scrollView.Post(() =>
    {
        int amount =
            _scrollView.Height;

        if (amount <= 0)
            amount = 1;

        _scrollView.ScrollBy(
            0,
            amount);
    });
}

}