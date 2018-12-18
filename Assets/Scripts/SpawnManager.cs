using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public enum PlayerTeam
{
    None,
    BlueTeam,

    RedTeam
}

public class SpawnManager : MonoBehaviour
{
    [SerializeField] private List<SpawnPoint> _sharedSpawnPoints = new List<SpawnPoint>();
    System.Random _random = new System.Random();
	float _closestDistance;
    [Tooltip("This will be used to calculate the second filter where algorithm looks for closest friends, if the friends are away from this value, they will be ignored")]
    [SerializeField] private float _maxDistanceToClosestFriend = 30;
    [Tooltip("This will be used to calculate the first filter where algorithm looks for enemies that are far away from this value. Only enemies which are away from this value will be calculated.")]
    [SerializeField] private float _minDistanceToClosestEnemy = 10;
    [Tooltip("This value is to prevent friendly player spawning on top of eachothers. If a player is within the range of this value to a spawn point, that spawn point will be ignored")]
    [SerializeField] private float _minMemberDistance = 2;
    [SerializeField] private List<SpawnPoint> _spawnPoints;


    public DummyPlayer PlayerToBeSpawned;
    public DummyPlayer[] DummyPlayers;

    /// <summary>
	/// Script ilk yüklendiğinde(oyun başlamadan önce), _sharedSpawnPoints listesini SpawnPoint tipindeki objelerin hepsiyle dolduruyoruz. 
	/// Aynı şekilde DummyPlayers arrayini de DummyPlayer tipindeki objelerin hepsiyle dolduruyoduruz.
	/// </summary>
    private void Awake()
    {
		_sharedSpawnPoints.AddRange(FindObjectsOfType<SpawnPoint>());

		DummyPlayers = FindObjectsOfType<DummyPlayer>();
    }

    #region SPAWN ALGORITHM

    /// <summary>
	/// Algoritmanın başladığı ilk yer. Buttona basılınca çağırılan fonksiyon.
	/// SpawnPoints adında bir değişken yaratılıyor. Bu değişken oyuncunun spawn olacağı yeri tutan bir variable. _sharedSpawnPoints'dan farkı ise _sharedSpawnPoints bütün spawn noktalarını tutarken,
    /// SpawnPoints sadece o ana uygunları, o an gidilebilicek spawn noktalarını tutuyor.
    /// CalculateDistancesForSpawnPoints() methodu çağırılıyor. Bu metotda bütün spawn noktalarının _distanceToClosestEnemy, _distanceToClosesFriend değişkenleri dolduruluyor.
    /// Bu metodun içinde ilk ve ikinci filtre çağırılıyor. Burda yapılan şey ise ilk filtre(GetSpawnPointsByDistanceSpawning) düşmana olan uzaklığa göre SpawnPointsin doldurulduğu metot.
    /// Eğer bu methoddan herhangi bir spawn noktası gelmezse yani herhangi bir nokta kriterlere uymadıysa, GetSpawnPointsBySquadSpawning() metodu devreye giriyor. Bu metot bizim ikinci filtremiz.
    /// Bu filtrede spawnPoints takım arkadaşlarına olan yakınlığa göre dolduruluyor.
    /// En son olarak spawnPoint değişkeni spawnPoints listesi tek elemanlıysa o elemanına eşitleniyor eğer spawnPointsde birden çok eleman varsa 0 ile ortadaki eleman arasındaki elemanlardan rastgele birine eşitleniyor.
	/// </summary>
    public SpawnPoint GetSharedSpawnPoint(PlayerTeam team)
    {
        List<SpawnPoint> _spawnPoints = new List<SpawnPoint>(_sharedSpawnPoints.Count);
        CalculateDistancesForSpawnPoints(team);
        GetSpawnPointsByDistanceSpawning(team, ref _spawnPoints);
        if (_spawnPoints.Count <= 0)
        {
            GetSpawnPointsBySquadSpawning(team, ref _spawnPoints);
        }
        SpawnPoint spawnPoint = _spawnPoints.Count <= 1 ? _spawnPoints[0] : _spawnPoints[_random.Next(0, (int)((float)_spawnPoints.Count * .5f))];
        spawnPoint.StartTimer();
        return spawnPoint;
    }
    /// <summary>
    /// Bu metot bizim ilk filtremiz. spawnPoints listesini boşaltıyoruz. Ve bütün spawn pointlerinin olduğu listeyi düşmana olan uzaklığı en fazla olan spawn noktası başta olacak şekilde sıralıyoruz.
    /// Bundan sonra ise, bütün spawn noktalarından en yakın düşman mesafesi, belirlediğimiz minimum mesafeden fazla olan noktalar kadar dönen bir for döngüsü açıyoruz.
    /// Bu döngünün içindeki if yapısında şuan dönen _sharedSpawnPoint elemanının DistanceToClosestFriend özelliğinin belirlediğimiz _minMemberDistance'dan(minimum olabilicek mesafe, spawn point ile DummyPlayer arası) fazla olmasına,
    /// Aynı şekilde DistanceToClosestEnemy'ye de bakıyoruz ve son olarak SpawnTimer'ın(bir yerde spawn olduktan sonra devreye giren timer) 0'a eşit veya 0 dan küçük olup olmadığına bakıyoruz.
    /// Bütün koşullar sağlanıyor ise, mevcut dönmekte olan _sharedSpawnPoints(bütün spawn noktaları) elemanını spawnPoints listemize ekliyoruz.
    /// </summary>
    private void GetSpawnPointsByDistanceSpawning(PlayerTeam team, ref List<SpawnPoint> suitableSpawnPoints)
    {
        if (suitableSpawnPoints == null)
        {
            suitableSpawnPoints = new List<SpawnPoint>();
        }
        suitableSpawnPoints.Clear();
        _sharedSpawnPoints.Sort(delegate (SpawnPoint a, SpawnPoint b)
        {
            if (a.DistanceToClosestEnemy == b.DistanceToClosestEnemy)
            {
                return 0;
            }
            if (a.DistanceToClosestEnemy < b.DistanceToClosestEnemy)
            {
                return 1;
            }
            return -1;
        });
        for (int i = 0; i < _sharedSpawnPoints.Count && _sharedSpawnPoints[i].DistanceToClosestEnemy >= _minDistanceToClosestEnemy; i++)
        {
            if (!(_sharedSpawnPoints[i].DistanceToClosestFriend <= _minMemberDistance) && !(_sharedSpawnPoints[i].DistanceToClosestEnemy <= _minMemberDistance) && _sharedSpawnPoints[i].SpawnTimer <= 0)
            {
                suitableSpawnPoints.Add(_sharedSpawnPoints[i]);
            }
        }
        suitableSpawnPoints.Reverse();
        Debug.Log(suitableSpawnPoints.Count);
    }

    private void GetSpawnPointsBySquadSpawning(PlayerTeam team, ref List<SpawnPoint> suitableSpawnPoints)
    {
        if (suitableSpawnPoints == null)
        {
            suitableSpawnPoints = new List<SpawnPoint>();
        }
        suitableSpawnPoints.Clear();
        _sharedSpawnPoints.Sort(delegate (SpawnPoint a, SpawnPoint b)
        {
            if (a.DistanceToClosestFriend == b.DistanceToClosestFriend)
            {
                return 0;
            }
            if (a.DistanceToClosestFriend > b.DistanceToClosestFriend)
            {
                return 1;
            }
            return -1;
        });
        for (int i = 0; i < _sharedSpawnPoints.Count && _sharedSpawnPoints[i].DistanceToClosestFriend <= _maxDistanceToClosestFriend; i++)
        {
            if (!(_sharedSpawnPoints[i].DistanceToClosestFriend <= _minMemberDistance) && !(_sharedSpawnPoints[i].DistanceToClosestEnemy <= _minMemberDistance) && _sharedSpawnPoints[i].SpawnTimer <= 0)
            {
                suitableSpawnPoints.Add(_sharedSpawnPoints[i]);
            }
        }
        if (suitableSpawnPoints.Count <= 0)
        {
            suitableSpawnPoints.Add(_sharedSpawnPoints[0]);
        }

    }

    private void CalculateDistancesForSpawnPoints(PlayerTeam playerTeam)
    {
        for (int i = 0; i < _sharedSpawnPoints.Count; i++)
        {
            _sharedSpawnPoints[i].DistanceToClosestFriend = GetDistanceToClosestMember(_sharedSpawnPoints[i].PointTransform.position, playerTeam);
            _sharedSpawnPoints[i].DistanceToClosestEnemy = GetDistanceToClosestMember(_sharedSpawnPoints[i].PointTransform.position, playerTeam == PlayerTeam.BlueTeam ? PlayerTeam.RedTeam : playerTeam == PlayerTeam.RedTeam ? PlayerTeam.BlueTeam : PlayerTeam.None);
        }
    }

    private float GetDistanceToClosestMember(Vector3 position, PlayerTeam playerTeam)
    {
        _closestDistance = 0;
        foreach (var player in DummyPlayers)
        {
            if (!player.Disabled && player.PlayerTeamValue != PlayerTeam.None && player.PlayerTeamValue == playerTeam && !player.IsDead())
            {
                float playerDistanceToSpawnPoint = Vector3.Distance(position, player.Transform.position);
                if (playerDistanceToSpawnPoint < _closestDistance || _closestDistance == 0)
                {
                    _closestDistance = playerDistanceToSpawnPoint;
                }
            }
        }
        return _closestDistance;
    }

    #endregion
	/// <summary>
	/// Test için paylaşımlı spawn noktalarından en uygun olanını seçer.
	/// Test oyuncusunun pozisyonunu seçilen spawn noktasına atar.
	/// </summary>
    public void TestGetSpawnPoint()
    {
    	SpawnPoint spawnPoint = GetSharedSpawnPoint(PlayerToBeSpawned.PlayerTeamValue);
    	PlayerToBeSpawned.Transform.position = spawnPoint.PointTransform.position;
    }

}