namespace PlayersSystems
{
    public class Player : PlayerController
    {
        public static Player Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }
    }
}
