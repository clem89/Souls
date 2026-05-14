using NUnit.Framework;

public class StaminaSystemTests
{
    [Test]
    public void Initial_CurrentEqualsMax()
    {
        var s = new StaminaSystem(100f);
        Assert.AreEqual(100f, s.Current, 0.001f);
    }

    [Test]
    public void TryConsume_ReducesStamina()
    {
        var s = new StaminaSystem(100f);
        bool ok = s.TryConsume(30f);
        Assert.IsTrue(ok);
        Assert.AreEqual(70f, s.Current, 0.001f);
    }

    [Test]
    public void TryConsume_ReturnsFalseWhenInsufficient()
    {
        var s = new StaminaSystem(100f);
        s.TryConsume(80f);
        bool ok = s.TryConsume(30f);
        Assert.IsFalse(ok);
        Assert.AreEqual(20f, s.Current, 0.001f);
    }

    [Test]
    public void TryConsume_NeverGoesBelowZero()
    {
        var s = new StaminaSystem(10f);
        s.TryConsume(9f);
        s.TryConsume(5f);
        Assert.AreEqual(1f, s.Current, 0.001f);
    }

    [Test]
    public void Recover_IncreasesStamina()
    {
        var s = new StaminaSystem(100f);
        s.TryConsume(50f);
        s.Recover(20f);
        Assert.AreEqual(70f, s.Current, 0.001f);
    }

    [Test]
    public void Recover_ClampsAtMax()
    {
        var s = new StaminaSystem(100f);
        s.TryConsume(10f);
        s.Recover(50f);
        Assert.AreEqual(100f, s.Current, 0.001f);
    }

    [Test]
    public void OnChanged_FiredWithCurrentValue()
    {
        var s = new StaminaSystem(100f);
        float received = -1f;
        s.OnChanged += v => received = v;
        s.TryConsume(30f);
        Assert.AreEqual(70f, received, 0.001f);
    }
}
