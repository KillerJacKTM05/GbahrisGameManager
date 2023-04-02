using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SafeZone
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(this.gameObject);
            }

        }
        [Header("GameLoop")]
        [SerializeField] private List<RobotTask> tasks = new List<RobotTask>();
        [SerializeField] private List<SpawnEvent> spawnEvents = new List<SpawnEvent>();
        [SerializeField] private GameSettings gameSettings;
        [SerializeField] private LevelComponent activeLevel;

        private PlayerController activePlayer;
        private Light worldLight;
        private Coroutine dailyCycle;
        private Coroutine mainLoop;
        private bool gameStarted = false;
        private bool isgameFinished = false;
        private bool lights = false;

        private int minuteCounter = 0;
        private int hourCounter = 8;
        private int[] hour = new int[2];
        private int[] minutes = new int[2];
        private List<Light> hospitalSpots = new List<Light>();
        private List<BaseNpcBehavior> activeNpc = new List<BaseNpcBehavior>();

        void Start()
        {
            InterfaceManager.Instance.objectivePanelUI.InitializeThis();
            //SpawnLevel();
            /*
            GetWorldLight();
            FindAndAddTasks(); 
            OrganizeEvents();
            AddPaths();
            SetHospitalSpotStatus(false, 3);
            */
        }
        public GameSettings GetGameSettings()
        {
            return gameSettings;
        }
        public void SetPlayer(PlayerController ply)
        {
            activePlayer = ply;
            activePlayer.SetBeforePose(activePlayer.transform.position);
            activePlayer.SetCurrentPose(activePlayer.transform.position);
        }
        public PlayerController GetPlayer()
        {
            return activePlayer;
        }
        public bool GetGameStarted()
        {
            return gameStarted;
        }
        public void SetGameStarted(bool state)
        {
            gameStarted = state;
            if (state)
            {
                ResetDailyTime();
            }
        }
        public bool GetIsGameFinished()
        {
            return isgameFinished;
        }
        public void RemoveAQuestFromTheList(RobotTask target)
        {
            for(int i = 0; i < tasks.Count; i++)
            {
                if(target == tasks[i])
                {
                    RobotTask temp = target;
                    tasks.RemoveAt(i);
                    target.Terminate();
                }
            }
        }
        public void ResetDailyTime()
        {
            /*hour[1] = GetGameSettings().gameStartingHour / 10;
            hour[0] = GetGameSettings().gameStartingHour % 10;
            minutes[1] = GetGameSettings().gameStartingMin / 10;
            minutes[0] = GetGameSettings().gameStartingMin % 10; */
            dailyCycle = StartCoroutine(GameDayCycle());
            mainLoop = StartCoroutine(MainGameLoop());
        }
        public int GetHour()
        {
            string hours = hour[1].ToString() + hour[0].ToString();
            return int.Parse(hours);
        }
        public int GetMinute()
        {
            string minute = minutes[1].ToString() + minutes[0].ToString();
            return int.Parse(minute);
        }
        private void GetWorldLight()
        {
            worldLight = activeLevel.environmentLight;
            Light[] Spots = activeLevel.gameObject.GetComponentsInChildren<Light>();
            foreach(Light spot in Spots)
            {
                if(spot.type == LightType.Point)
                {
                    hospitalSpots.Add(spot);
                }
                else
                {
                    continue;
                }
            }
        }
        private void SetHospitalSpotStatus(bool state, int iteration = 1)
        {
            for(int i = 0; i < hospitalSpots.Count; i++)
            {
                if(i % iteration == 0)
                {
                    hospitalSpots[i].gameObject.SetActive(!state);
                }
                else
                {
                    hospitalSpots[i].gameObject.SetActive(state);
                }
            }
        }
        public void SelectLevel(int levelID)
        {
            SpawnLevel(levelID);
        }
        private void SpawnLevel(int id)
        {
            if(activeLevel == null)
            {
                //int rand = Random.Range(0, GetGameSettings().levelList.Count);
                activeLevel = Instantiate(GetGameSettings().levelList[id].levelPrefab);
                //set game starting hour and minute via spawned level.
                if (activeLevel.scenarioStartHour != 8 && activeLevel.scenarioStartMinute != 0)
                {
                    //set the game starting time.
                    hourCounter = activeLevel.scenarioStartHour;
                    minuteCounter = activeLevel.scenarioStartMinute;
                }
                else
                {
                    //set the game starting time with gameSettings.
                    hourCounter = GetGameSettings().gameStartingHour;
                    minuteCounter = GetGameSettings().gameStartingMin;
                }
                GetWorldLight();
                FindAndAddTasks();
                OrganizeEvents();
                AddPaths();
                GetPlayer().GetComponent<Rigidbody>().useGravity = true;
            }
        }
        public int GetActiveTaskCount()
        {
            return tasks.Count;
        }
        public string GetActiveLevelName()
        {
            return activeLevel.name;
        }
        private void FindAndAddTasks()
        {
            RobotTask[] taksOnField = FindObjectsOfType<RobotTask>();
            foreach(RobotTask t in taksOnField)
            {
                tasks.Add(t);
                if (t.isActiveNow)
                {
                    InterfaceManager.Instance.objectivePanelUI.AddTask(t);
                }
            }
        }
        //for now i added a simple bubble sort here for organizing the spawn event times. We will always call and execute the first element of the events, therefore we need to know, the first element is earliest
        private void OrganizeEvents()
        {
            //first, find events
            SpawnEvent[] events = FindObjectsOfType<SpawnEvent>();
            foreach(SpawnEvent e in events)
            {
                spawnEvents.Add(e);
            }
            //then organize
            if (spawnEvents.Count > 1)
            {
                for (int i = 0; i < spawnEvents.Count; i++)
                {
                    for (int j = spawnEvents.Count - 1; j > 0; j--)
                    {
                        if (spawnEvents[j].GetEventHour() < spawnEvents[j - 1].GetEventHour())
                        {
                            SpawnEvent temp = spawnEvents[j];
                            spawnEvents[j] = spawnEvents[j - 1];
                            spawnEvents[j - 1] = temp;
                        }
                        else if (spawnEvents[j].GetEventHour() == spawnEvents[j - 1].GetEventHour())
                        {
                            if (spawnEvents[j].GetEventMinute() < spawnEvents[j - 1].GetEventMinute())
                            {
                                SpawnEvent temp = spawnEvents[j];
                                spawnEvents[j] = spawnEvents[j - 1];
                                spawnEvents[j - 1] = temp;
                            }
                        }
                        else continue;
                    }
                }
            }
            else { }
        }
        public void AddNpc(BaseNpcBehavior baseNpc)
        {
            activeNpc.Add(baseNpc);
        }
        public void AddFreeRoam(RobotTask free)
        {
            tasks.Add(free);
        }
        public void DeleteMe(BaseNpcBehavior baseNpc)
        {
            if(!baseNpc.isStatic)
            {
                for (int i = 0; i < activeNpc.Count; i++)
                {
                    if (baseNpc.name == activeNpc[i].name)
                    {
                        activeNpc.RemoveAt(i);
                    }
                }
            }
            Destroy(baseNpc.gameObject);
        }
        private void ExecuteTargetEvent()
        {
            //executee the event
            for(int i = 0; i < spawnEvents[0].deployAmount; i++)
            {
                var tempNpc = GetGameSettings().GetRandomNpc();
                spawnEvents[0].InvokeTheEvent(tempNpc, GetGameSettings().pointHolder.GetSpawnPoint());
            }
            DeleteTheEvent();
        }
        private void DeleteTheEvent(int index = 0)
        {
            SpawnEvent temp = spawnEvents[index];
            spawnEvents.RemoveAt(index);
            Destroy(temp.gameObject);
        }
        //enter possible patrol paths here.
        private void AddPaths()
        {
            GetGameSettings().pointHolder.AddNewPath(1, 4);
        }
        private void SetTimeValuesToUsable(int minute, int hourTime)
        {
            if(minute > 9)
            {
                minutes[1] = minute / 10;
                minutes[0] = minute % 10;
            }
            else
            {
                minutes[1] = 0;
                minutes[0] = minute;
            }
            if(hourTime > 9)
            {
                hour[1] = hourTime / 10;
                hour[0] = hourTime % 10;
            }
            else
            {
                hour[1] = 0;
                hour[0] = hourTime;
            }
        }
        /// <summary>
        /// we will arrange the x rotation value of the sun here.
        /// </summary>
        private IEnumerator ChangeSunPosition(float timer)
        {
            float angle = 180f / (timer * 12f * 60f);
            //this line for starting angle.
            float startingAngle = angle * 60f * (activeLevel.scenarioStartHour - GetGameSettings().gameStartingHour);
            worldLight.transform.Rotate(new Vector3(startingAngle, 0, 0));

            while (gameStarted)
            {
                if (!gameStarted)
                {
                    yield break;
                }

                if(GetHour() > 4 && GetHour() < 23)
                {
                    if (worldLight.transform.rotation.eulerAngles.x <= 179.5f)
                    {
                        worldLight.transform.Rotate(new Vector3(angle, 0, 0));
                    }
                }
                yield return new WaitForSeconds(timer);
            }
        }
        private IEnumerator GameDayCycle()
        {
            float timer = gameSettings.realWorldTimeForEachGameHour / 60f;
            StartCoroutine(ChangeSunPosition(timer));
            while (gameStarted)
            {
                if(minuteCounter < 59)
                {
                    minuteCounter++;
                }
                else
                {
                    minuteCounter = 0;

                    if(hourCounter < 23)
                    {
                        hourCounter++;
                    }
                    else
                    {
                        hourCounter = 0;
                    }
                }
                //gameloop ending sequence.
                if (hourCounter == 7 && minuteCounter == 59)
                {
                    gameStarted = false;
                    isgameFinished = true;
                    yield break;
                }
                SetTimeValuesToUsable(minuteCounter, hourCounter);
                InterfaceManager.Instance.InGameUI.UpdateHourString(hour, minutes);
                yield return new WaitForSeconds(timer);
            }
        }
        private IEnumerator MainGameLoop()
        {
            float timer = gameSettings.realWorldTimeForEachGameHour / 60f;
            Vector3 playerCurPos = GetPlayer().GetCurrentPos();
            while (gameStarted)
            {
                //this if else for hospital light event
                if (!lights)
                {
                    if ((GetHour() >= GetGameSettings().ParseTheHour(true) && GetMinute() >= GetGameSettings().ParseTheMinutes(true)) && GetHour() < GetGameSettings().ParseTheHour(false))
                    {
                        SetHospitalSpotStatus(true, 3);
                        lights = !lights;
                    }
                }
                else
                {
                    if ((GetHour() >= GetGameSettings().ParseTheHour(false) && GetMinute() >= GetGameSettings().ParseTheMinutes(false)))
                    {
                        SetHospitalSpotStatus(false, 3);
                        lights = !lights;
                    }
                }

                if(spawnEvents.Count >= 1)
                {
                    if (spawnEvents[0].GetEventHour() == GetHour() && spawnEvents[0].GetEventMinute() == GetMinute())
                    {
                        ExecuteTargetEvent();
                    }
                }

                //scenario is now ended. Show the ui and give player to select continue or quit.
                if(GetActiveTaskCount() < 1)
                {
                    InterfaceManager.Instance.StopGame(1);
                    InterfaceManager.Instance.SetRoutineBlocker(true);
                }

                playerCurPos = GetPlayer().transform.position;
                GetPlayer().SetCurrentPose(playerCurPos);
                //for map displaying we need to update crossair.
                InterfaceManager.Instance.UpdateCrossair();
                //for updating playerworldpos in data management system
                DataManager.Instance.UpdatePlayerWorldData(GetPlayer().transform.position, GetPlayer().GetRotationValues(), GetPlayer().CalculateAndGetVelocity());
                if (isgameFinished)
                {
                    DataManager.Instance.WriteToFile();
                    yield break;
                }
                yield return new WaitForSeconds(timer);
                GetPlayer().SetBeforePose(playerCurPos);
            }

        }
    }
}

