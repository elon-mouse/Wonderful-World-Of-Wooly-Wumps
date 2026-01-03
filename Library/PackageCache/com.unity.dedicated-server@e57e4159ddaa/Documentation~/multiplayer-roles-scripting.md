# Use a script to access the active multiplayer role

You can access the multiplayer role that the Unity Editor or the current build uses in Play mode in a script. This allows you to write custom logic that runs only on the server or only on the client.

The example below demonstrates custom logic that exists when a process runs on the server or as a client.

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Multiplayer;

public class SceneBootstrap : MonoBehaviour
{
    void Start()
    {
        var role = MultiplayerRolesManager.ActiveMultiplayerRoleMask;

        if (role == MultiplayerRoleFlags.Server)
        {
            Debug.Log("Running SceneBootstrap in the server");
            SceneManager.LoadScene("ServerScene");
        }
        else if (role == MultiplayerRoleFlags.Client)
        {
            Debug.Log("Running SceneBootstrap in a client");
            SceneManager.LoadScene("LobbyScene");
        }
    }
}
```
