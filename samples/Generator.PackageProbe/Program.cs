using Esolang.Piet;

Console.WriteLine("Package probe start");
PietProbe.Run();

partial class PietProbe
{
    /// <summary>
    /// Executes the hello-world sample to verify package generator wiring.
    /// </summary>
    [GeneratePietMethod("hello-world.png")]
    public static partial void Run();
}
