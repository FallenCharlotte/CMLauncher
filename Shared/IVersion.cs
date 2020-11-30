public interface IVersion
{
    int VersionNumber { get; }
    string VersionServer { get; }

    void Update(int version, string server);
}