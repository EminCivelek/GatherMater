using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class UGSManager : MonoBehaviour
{
    public static UGSManager Instance { get; private set; }

    bool _isSignedIn;
    public bool IsSignedIn => _isSignedIn;
    public string PlayerId => _isSignedIn ? AuthenticationService.Instance.PlayerId : null;

    public event Action OnSignedIn;
    public event Action<string> OnSignInFailed;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    async void Start()
    {
        await InitializeAsync();
    }

    async Task InitializeAsync()
    {
        try
        {
            await UnityServices.InitializeAsync();
            await SignInAnonymouslyAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"[UGS] Initialization failed: {e.Message}");
            OnSignInFailed?.Invoke(e.Message);
        }
    }

    async Task SignInAnonymouslyAsync()
    {
        if (AuthenticationService.Instance.IsSignedIn) { OnSignedIn?.Invoke(); return; }

        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            _isSignedIn = true;
            Debug.Log($"[UGS] Signed in. Player ID: {PlayerId}");
            OnSignedIn?.Invoke();
        }
        catch (AuthenticationException e)
        {
            Debug.LogError($"[UGS] Auth error: {e.Message}");
            OnSignInFailed?.Invoke(e.Message);
        }
        catch (RequestFailedException e)
        {
            Debug.LogError($"[UGS] Request failed: {e.Message}");
            OnSignInFailed?.Invoke(e.Message);
        }
    }
}
