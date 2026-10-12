namespace Splatform;

/// <summary>
/// Compile-time stand-in for the game's <c>Splatform.PlatformUserID</c>, which the
/// published reference packages do not ship. <c>Minimap.AddPin</c> declares an
/// optional parameter of this type, so the compiler must see the type even though
/// the mod never names it. Only <c>default</c> is ever passed, which compiles to an
/// <c>initobj</c> on whatever the real struct is, so the shape here does not matter.
/// At runtime the mod binds to the game's <c>Splatform, Version=0.0.0.0</c> by simple
/// name. This assembly is never packaged: the release copies two named DLLs.
/// </summary>
public struct PlatformUserID
{
}
