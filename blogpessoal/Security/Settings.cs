namespace blogpessoal.Security {
    public class Settings {
        private static string secret = "B51AFDCBC981C3D811D557F2CB3954743F9FFC8BE6ACE61EF63051DF218AACAE";
        public static string Secret { get => secret; set => secret = value; }
    }
}
