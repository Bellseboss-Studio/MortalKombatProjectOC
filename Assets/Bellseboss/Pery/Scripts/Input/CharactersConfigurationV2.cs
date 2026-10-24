using System;
using System.Collections.Generic;
using _Scripts.Player;
using UnityEngine;

namespace Bellseboss.Pery.Scripts.Input
{
    [CreateAssetMenu(menuName = "Bellseboss/CharacterConfiguration V2")]
    public class CharactersConfigurationV2 : ScriptableObject
    {
        [SerializeField] private CharacterV2[] characters;
        private Dictionary<string, CharacterV2> idToCharacter;

        private void Awake()
        {
            idToCharacter = new Dictionary<string, CharacterV2>(characters.Length);
            foreach (var character in characters)
            {
                idToCharacter.Add(character.Id, character);
            }
        }

        public CharacterV2 GetCharacterPrefabById(string id)
        {
            if (!idToCharacter.TryGetValue(id, out var characterPrefab))
            {
                throw new Exception($"Character with id {id} does not exit");
            }
            return characterPrefab;
        }
    }
}