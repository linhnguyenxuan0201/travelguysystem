namespace TripCompass.Application.Services
{
    public class SentimentAnalysisService
    {
        // Từ khóa tích cực
        private readonly string[] _positiveKeywords = new[]
        {
            "tuyệt vời", "xuất sắc", "rất tốt", "tốt", "ngon", "đẹp", "thích", "hài lòng",
            "tuyệt", "tốt lắm", "recommend", "nên thử", "đáng giá", "xứng đáng", "ưng ý",
            "thú vị", "hấp dẫn", "chất lượng", "tuyệt hảo", "hoàn hảo", "ấn tượng",
            "tốt", "hay", "ok", "ổn", "được", "thích hợp", "phù hợp", "đáng", "nên"
        };

        // Từ khóa tiêu cực
        private readonly string[] _negativeKeywords = new[]
        {
            "tệ", "dở", "không tốt", "tệ hại", "thất vọng", "không hài lòng", "kém",
            "xấu", "không ngon", "không đáng", "lừa đảo", "giả", "kém chất lượng",
            "tồi tệ", "không nên", "tránh", "tệ quá", "không ổn", "không được",
            "chán", "nhàm chán", "không thích", "ghét", "phản đối", "không hợp"
        };

        public SentimentResult Analyze(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new SentimentResult { Sentiment = SentimentType.Neutral, Score = 0 };

            var lowerText = text.ToLower();
            int positiveCount = 0;
            int negativeCount = 0;

            // Đếm từ khóa tích cực
            foreach (var keyword in _positiveKeywords)
            {
                if (lowerText.Contains(keyword))
                    positiveCount++;
            }

            // Đếm từ khóa tiêu cực
            foreach (var keyword in _negativeKeywords)
            {
                if (lowerText.Contains(keyword))
                    negativeCount++;
            }

            // Xác định sentiment
            if (positiveCount > negativeCount)
            {
                return new SentimentResult 
                { 
                    Sentiment = SentimentType.Positive, 
                    Score = positiveCount - negativeCount 
                };
            }
            else if (negativeCount > positiveCount)
            {
                return new SentimentResult 
                { 
                    Sentiment = SentimentType.Negative, 
                    Score = negativeCount - positiveCount 
                };
            }
            else
            {
                return new SentimentResult 
                { 
                    Sentiment = SentimentType.Neutral, 
                    Score = 0 
                };
            }
        }
    }

    public class SentimentResult
    {
        public SentimentType Sentiment { get; set; }
        public int Score { get; set; }
    }

    public enum SentimentType
    {
        Positive,
        Negative,
        Neutral
    }
}
