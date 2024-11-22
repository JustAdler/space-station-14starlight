using Content.Server.GameTicking;
using Content.Shared.Starlight.CCVar;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Movement.Components;
using Robust.Server.Player;
using Robust.Shared.Configuration;

namespace Content.Server.Starlight.GameTicking;

public sealed class PeacefulRoundEndSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private bool _isEnabled = false;

    public override void Initialize()
    {
        base.Initialize();
        _cfg.OnValueChanged(StarlightCCVars.PeacefulRoundEnd, v => _isEnabled = v, true);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEnded);
    }

    private void OnRoundEnded(RoundEndTextAppendEvent ev)
    {
        if (!_isEnabled) return;
        foreach (var mob in EntityQuery<MobMoverComponent>())
        {
            EnsureComp<PacifiedComponent>(mob.Owner);
        }
    }
}