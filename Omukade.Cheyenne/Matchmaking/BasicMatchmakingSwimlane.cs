﻿using MatchLogic;

namespace Omukade.Cheyenne.Matchmaking
{
    internal class BasicMatchmakingSwimlane : IMatchmakingSwimlane
    {
        public BasicMatchmakingSwimlane(GameplayType gameplayType, GameMode format, MatchmakingCompleteCallback matchmakingCompleteCallback)
        {
            this.gameplayType = gameplayType;
            this.format = format;
            MatchMakingCompleteCallback = matchmakingCompleteCallback;
        }

        public GameplayType gameplayType { get; init; }
        public GameMode format { get; init; }

        public static uint GetFormatKey(GameplayType gameplayType, GameMode format)
        {
            return unchecked((uint)gameplayType << 0xffff | (uint)format);
        }

        internal Queue<PlayerMetadata> enqueuedPlayers = new Queue<PlayerMetadata>(2);

        public MatchmakingCompleteCallback MatchMakingCompleteCallback { get; init; }

        /// <inheritdoc/>
        public void EnqueuePlayer(PlayerMetadata playerMetadata)
        {
            foreach (PlayerMetadata player in enqueuedPlayers)
            {
                if (playerMetadata == player || playerMetadata.PlayerId == player.PlayerId)
                {
                    throw new InvalidOperationException("Player is already enqueued for matchmaking");
                }
            }

            enqueuedPlayers.Enqueue(playerMetadata);

            if (enqueuedPlayers.Count >= 2)
            {
                PlayerMetadata playerMetadata1 = enqueuedPlayers.Dequeue();
                PlayerMetadata playerMetadata2 = enqueuedPlayers.Dequeue();
                MatchMakingCompleteCallback(this, playerMetadata1, playerMetadata2);
            }
        }

        /// <inheritdoc/>
        public void RemovePlayerFromMatchmaking(PlayerMetadata playerMetadata)
        {
            bool playerIsQueued = false;
            bool doPlayersMatch(PlayerMetadata queuedPlayer, PlayerMetadata playerToRemove) => queuedPlayer == playerToRemove || queuedPlayer.PlayerId == playerToRemove.PlayerId;

            // Determine if the player is in queue. If they're not, don't bother trying to rebuild the queue.
            foreach (PlayerMetadata player in enqueuedPlayers)
            {
                if (doPlayersMatch(player, playerMetadata))
                {
                    playerIsQueued = true;
                    break;
                }
            }

            if (!playerIsQueued) return;

            Queue<PlayerMetadata> newQueue = new Queue<PlayerMetadata>(2);
            foreach (PlayerMetadata player in enqueuedPlayers)
            {
                if (doPlayersMatch(player, playerMetadata))
                {
                    continue;
                }
                else
                {
                    newQueue.Enqueue(player);
                }
            }


            enqueuedPlayers = newQueue;
        }
    }

    public delegate void MatchmakingCompleteCallback(IMatchmakingSwimlane swimlane, PlayerMetadata player1, PlayerMetadata player2);
}
