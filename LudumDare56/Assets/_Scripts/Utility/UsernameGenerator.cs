using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Utils {
    public class UsernameGenerator : Singleton<UsernameGenerator>
    {
        [Header("NOTE: This script can be added for each type of thing that needs to be named!")]
        [Header("Word Lists")]
        [SerializeField]
        [Tooltip("The comma-separated list of descriptors to generate names from.")]
        private TextAsset listOfAdjectives;
        [SerializeField]
        [Tooltip("The comma-separated list of nouns to generate names from.")]
        private TextAsset listOfNouns;
        [SerializeField]
        [Tooltip("The character sequence to use to split the adjectives and nouns with.")]
        private string delimiter = ",";
        [SerializeField]
        [Tooltip("The character sequence to add between the adjective and the noun.")]
        private string separator = "";

        private string[] adjectivesList;
        private string[] nounsList;

        private void Start()
        {
            adjectivesList = listOfAdjectives.text.Split(delimiter);
            nounsList = listOfNouns.text.Split(delimiter);
        }

        public string GenerateName()
        {
            // Get random name
            StringBuilder generatedName = new();
            generatedName.Append(GetRandomAdjectiveFromFile());
            generatedName.Append(separator);
            generatedName.Append(GetRandomNounFromFile());

            if (generatedName.ToString() == string.Empty)
            {
                return "Rustle";
            }
            else
            {
                return generatedName.ToString();
            }
        }

        private string GetRandomAdjectiveFromFile()
        {
            return adjectivesList[Random.Range(0, adjectivesList.Length)];
        }

        private string GetRandomNounFromFile()
        {

            return nounsList[Random.Range(0, nounsList.Length)];
        }
    }
}