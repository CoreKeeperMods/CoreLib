# Security Module

This is not a typical module. It does not provide any public API.

This module performs server sided validation on Chat Commands performed by clients. Clients that do not have admin privileges are restricted from using cheat commands. Also some other restrictions are possible.

In config server/host can change a few settings to:
- Should chat commands be allowed?
- Should unknown commands  be allowed? (IE server does not know about it)
- Should self cheats (Cheats that apply only to the player) be allowed?

By default all of these are enabled.


If you do not have chat command installed you should force this module to load for this functionality to be enabled:

`ForceModuleLoad = SecurityModule`

# NOTE
This module does not provide 100% guarantee that clients can't cheat. Unfortunately the checks can be bypassed by individuals with sufficient desire.

If you must ensure that no one can cheat, use game's guest mode, as it's much more robust in forbidding unauthorized access to the world. 