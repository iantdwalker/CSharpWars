﻿using System.Linq;
using System.Threading.Tasks;
using CSharpWars.Common.Extensions;
using CSharpWars.Common.Helpers.Interfaces;
using CSharpWars.Enums;
using CSharpWars.ScriptProcessor.Middleware.Interfaces;
using CSharpWars.ScriptProcessor.Moves;

namespace CSharpWars.ScriptProcessor.Middleware
{
    public class Postprocessor : IPostprocessor
    {
        private readonly IRandomHelper _randomHelper;

        public Postprocessor(IRandomHelper randomHelper)
        {
            _randomHelper = randomHelper;
        }

        public Task Go(ProcessingContext context)
        {
            var botProperties = context.GetOrderedBotProperties();
            foreach (var botProperty in botProperties)
            {
                var bot = context.Bots.Single(x => x.Id == botProperty.BotId);
                var botResult = Move.Build(botProperty, _randomHelper).Go();
                bot.Orientation = botResult.Orientation;
                bot.X = botResult.X;
                bot.Y = botResult.Y;
                bot.CurrentHealth = botResult.CurrentHealth;
                bot.CurrentStamina = botResult.CurrentStamina;
                bot.Move = botResult.Move;
                bot.Memory = botResult.Memory.Serialize();

                context.UpdateBotProperties(bot);

                foreach (var otherBot in context.Bots.Where(x => x.Id != bot.Id))
                {
                    otherBot.CurrentHealth -= botResult.GetInflictedDamage(otherBot.Id);
                    var teleportation = botResult.GetTeleportation(otherBot.Id);
                    if (teleportation != (-1, -1))
                    {
                        otherBot.X = teleportation.X;
                        otherBot.Y = teleportation.Y;
                    }
                    var otherBotProperties = botProperties.Single(x => x.BotId == otherBot.Id);
                    otherBotProperties.Update(otherBot);
                }
            }

            foreach (var bot in context.Bots)
            {
                if (bot.CurrentHealth <= 0)
                {
                    bot.CurrentHealth = 0;
                    bot.Move = PossibleMoves.Died;
                }

                if (bot.CurrentStamina <= 0)
                {
                    bot.CurrentStamina = 0;
                }
            }

            return Task.CompletedTask;
        }
    }
}