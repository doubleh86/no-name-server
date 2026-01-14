namespace WorldServer.GameObjects;

public interface IAutoSavable
{
    bool IsSaveDirty();
    void ClearSaveDirty();
    void MarkSaveDirty();
    void OnAutoSaveSuccess();
}