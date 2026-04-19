using Esolang.Piet;

Console.WriteLine("Package probe start");
PietProbe.Run();

partial class PietProbe
{
    [GeneratePietMethod("hello-world.png")]
    public static partial void Run();
}
