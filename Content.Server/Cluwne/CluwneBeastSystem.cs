using Content.Shared.Mobs;
using Content.Shared.Stealth.Components;
using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using Content.Server.Emoting.Systems;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Cluwne;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Interaction.Components;

namespace Content.Server.Cluwne;

public sealed class CluwneBeastSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly AutoEmoteSystem _autoEmote = default!;



    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CluwneBeastComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<CluwneBeastComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<CluwneBeastComponent, MobStateChangedEvent>(OnMobState);
        SubscribeLocalEvent<CluwneBeastComponent, EmoteEvent>(OnEmote, before:
        new[] { typeof(VocalSystem), typeof(BodyEmotesSystem) });
    }

    /// <summary>
    /// On death removes active comps and gives genetic damage to prevent cloning, reduce this to allow cloning.
    /// </summary>
    private void OnMobState(EntityUid uid, CluwneBeastComponent component, MobStateChangedEvent args)
    {

        if (args.NewMobState == MobState.Dead || args.NewMobState == MobState.Critical)
        {
            RemComp<AutoEmoteComponent>(uid);
            RemComp<StealthOnMoveComponent>(uid);
        }

        else
        {
            EnsureComp<AutoEmoteComponent>(uid);
            _autoEmote.AddEmote(uid, "CluwneGiggle");
            EnsureComp<StealthOnMoveComponent>(uid);
        }
    }

    public EmoteSoundsPrototype? EmoteSounds;

    /// <summary>
    /// OnStartup gives the cluwne outfit, ensures clumsy, gives name prefix and makes sure emote sounds are laugh.
    /// </summary>
    private void OnComponentStartup(EntityUid uid, CluwneBeastComponent component, ComponentStartup args)
    {
        if (component.EmoteSoundsId == null)
            return;

        _prototypeManager.TryIndex(component.EmoteSoundsId, out EmoteSounds);
        EnsureComp<AutoEmoteComponent>(uid);
        _autoEmote.AddEmote(uid, "CluwneGiggle");
        EnsureComp<ClumsyComponent>(uid);
        Spawn(component.BlueSpaceId, Transform(uid).Coordinates);
    }

    /// <summary>
    /// Handles the timing on autoemote as well as falling over and honking.
    /// </summary>
    private void OnEmote(EntityUid uid, CluwneBeastComponent component, ref EmoteEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = _chat.TryPlayEmoteSound(uid, EmoteSounds, args.Emote);

        if (_robustRandom.Prob(component.GiggleRandomChance))
        {
            _audio.PlayPvs(component.SpawnSound, uid);
            _chat.TrySendInGameICMessage(uid, "honks", InGameICChatType.Emote, false, false);
        }

        else if (_robustRandom.Prob(component.KnockChance))
        {
            _audio.PlayPvs(component.KnockSound, uid);
            _stunSystem.TryParalyze(uid, TimeSpan.FromSeconds(component.ParalyzeTime), true);
            _chat.TrySendInGameICMessage(uid, "spasms", InGameICChatType.Emote, false, false);
        }
    }
    private void OnMeleeHit(EntityUid uid, CluwneBeastComponent component, MeleeHitEvent args)
    {
        foreach (var entity in args.HitEntities)
        {
            if (HasComp<HumanoidAppearanceComponent>(entity)
                && !_mobStateSystem.IsDead(entity)
                && _robustRandom.Prob(component.Cluwinification))
            {
                EnsureComp<CluwneComponent>(entity);
            }
        }
    }
}
