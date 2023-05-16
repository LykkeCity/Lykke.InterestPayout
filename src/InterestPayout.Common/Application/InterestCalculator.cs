using System;
using Common;

namespace InterestPayout.Common.Application
{
    public static class InterestCalculator
    {
        /// <summary> Calculate interest payout. </summary>
        /// <remarks> Whenever number of fractional digits after calculation is higher than the specified accuracy, resulting amount will be rounded down, i.e. to zero.</remarks>
        /// <param name="balance">A non-negative number which represents base balance</param>
        /// <param name="interestRate">Positive or negative interest rate, represented in percents</param>
        /// <param name="accuracy">Non-negative desired maximum number of fractional digits in the resulting interest payment amount</param>
        /// <returns>Positive or negative interest payment amount. Return type is <c>double</c> as it is what Matching Engine connector expects.</returns>
        /// <exception cref="InvalidOperationException">
        /// <list type="bullet">
        /// <item><description>Input balance is negative or zero;</description></item>
        /// <item><description>Input interest rate is lower than minus 100%, which would mean subtracting more than current total balance;</description></item>
        /// <item><description>Input accuracy is negative</description></item>
        /// </list>
        /// </exception>
        /// <exception cref="OverflowException">
        /// <list type="bullet">
        /// <item><description>Target interest payment amount is less than <c>decimal.MinValue</c> or greater than <c>decimal.MaxValue</c></description></item>
        /// </list>
        /// </exception>
        public static double CalculateInterest(decimal balance, decimal interestRate, int accuracy)
        {
            if (balance < decimal.Zero)
                throw new InvalidOperationException($"Base balance has to be a positive number, but was: '{balance}'");
            
            if (interestRate < -100)
                throw new InvalidOperationException($"Interest rate cannot be less than minus one hundred percent, but was: '{interestRate}'");

            if (accuracy < 0)
                throw new InvalidOperationException($"Accuracy cannot be negative, but was: '{accuracy}'");

            var interest = decimal.Divide(interestRate, 100);
            var decimalAmount = decimal.Multiply(balance, interest);
            var roundedDecimalAmount = decimalAmount.TruncateDecimalPlaces(accuracy);
            return Convert.ToDouble(roundedDecimalAmount);
        }
    }
}
