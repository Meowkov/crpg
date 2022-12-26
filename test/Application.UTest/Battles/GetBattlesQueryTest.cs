﻿using Crpg.Application.Battles.Queries;
using Crpg.Domain.Entities;
using Crpg.Domain.Entities.Battles;
using Crpg.Domain.Entities.Parties;
using Crpg.Domain.Entities.Settlements;
using Crpg.Domain.Entities.Users;
using NUnit.Framework;

namespace Crpg.Application.UTest.Battles;

public class GetBattlesQueryTest : TestBase
{
    [Test]
    public async Task ShouldGetBattlesMatchingThePhases()
    {
        Battle[] battles =
        {
            new()
            {
                Region = Region.Na,
                Phase = BattlePhase.Hiring,
                Fighters =
                {
                    new BattleFighter
                    {
                        Side = BattleSide.Attacker,
                        Commander = true,
                        Party = new Party { Troops = 20.9f, User = new User() },
                    },
                    new BattleFighter
                    {
                        Side = BattleSide.Attacker,
                        Commander = false,
                        Party = new Party { Troops = 15.8f, User = new User() },
                    },
                    new BattleFighter
                    {
                        Side = BattleSide.Defender,
                        Commander = false,
                        Party = new Party { Troops = 35.7f, User = new User() },
                    },
                    new BattleFighter
                    {
                        Side = BattleSide.Defender,
                        Commander = true,
                        Party = new Party { Troops = 10.6f, User = new User() },
                    },
                },
            },
            new()
            {
                Region = Region.Na,
                Phase = BattlePhase.Live,
                Fighters =
                {
                    new BattleFighter
                    {
                        Side = BattleSide.Attacker,
                        Commander = true,
                        Party = new Party { Troops = 100.5f, User = new User() },
                    },
                    new BattleFighter
                    {
                        Side = BattleSide.Defender,
                        Commander = true,
                        Settlement = new Settlement
                        {
                            Name = "toto",
                            Troops = 12,
                        },
                    },
                    new BattleFighter
                    {
                        Side = BattleSide.Defender,
                        Commander = false,
                        Party = new Party { Troops = 35.6f, User = new User() },
                    },
                },
            },
            new() { Region = Region.Na, Phase = BattlePhase.Preparation },
            new() { Region = Region.Eu, Phase = BattlePhase.Hiring },
            new() { Region = Region.As, Phase = BattlePhase.Live },
            new() { Region = Region.Na, Phase = BattlePhase.End },
        };
        ArrangeDb.Battles.AddRange(battles);
        await ArrangeDb.SaveChangesAsync();

        GetBattlesQuery.Handler handler = new(ActDb, Mapper);
        var res = await handler.Handle(new GetBattlesQuery
        {
            Region = Region.Na,
            Phases = new[] { BattlePhase.Hiring, BattlePhase.Live },
        }, CancellationToken.None);

        Assert.IsNull(res.Errors);

        var battlesVm = res.Data!;
        Assert.AreEqual(2, battlesVm.Count);

        Assert.AreEqual(Region.Na, battlesVm[0].Region);
        Assert.AreEqual(BattlePhase.Hiring, battlesVm[0].Phase);
        Assert.IsNotNull(battlesVm[0].Attacker);
        Assert.IsNotNull(battlesVm[0].Attacker.Party);
        Assert.AreEqual(35, battlesVm[0].AttackerTotalTroops);
        Assert.IsNotNull(battlesVm[0].Defender);
        Assert.IsNotNull(battlesVm[0].Defender!.Party);
        Assert.AreEqual(45, battlesVm[0].DefenderTotalTroops);

        Assert.AreEqual(Region.Na, battlesVm[1].Region);
        Assert.AreEqual(BattlePhase.Live, battlesVm[1].Phase);
        Assert.IsNotNull(battlesVm[1].Attacker);
        Assert.IsNotNull(battlesVm[1].Attacker.Party);
        Assert.AreEqual(100, battlesVm[1].AttackerTotalTroops);
        Assert.AreEqual(47, battlesVm[1].DefenderTotalTroops);
        Assert.IsNotNull(battlesVm[1].Defender);
        Assert.IsNotNull(battlesVm[1].Defender!.Settlement);
    }
}
