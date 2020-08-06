using System;
using System.Collections.Generic;
using System.Linq;
using PetaPoco;
using Snitz.Base;
using SnitzDataModel.Database;

namespace SiteManage.Models
{
    public class PaypalRepository : IDisposable
    {
        private SnitzDataContext _context;

        public PaypalRepository()
        {
            _context = new SnitzDataContext();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<PayPalProduct> GetProducts()
        {
            return _context.Query<PayPalProduct>("SELECT * FROM FORUM_PRODUCTS");

        }
        public PayPalProduct GetProductById(int productid)
        {
            return _context.SingleOrDefault<PayPalProduct>("WHERE PRODUCT_ID=@0",productid);

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="product"></param>
        /// <returns></returns>
        public void AddProduct(PayPalProduct product)
        {
            _context.Save(product);
        }
        public void DeleteProduct(int productid)
        {
            _context.Delete<PayPalProduct>(productid);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<PayPalDonation> GetDonations()
        {
            var sql = new Sql();
            sql.Select("D.*, M.M_FIRSTNAME AS Firstname,M.M_LASTNAME AS Lastname,M.M_NAME AS Username");
            sql.From("FORUM_DONATION D");
            sql.LeftJoin("FORUM_MEMBERS M").On("D.DONATION_MEMBER=M.MEMBER_ID");
            sql.OrderBy("D.DONATION_DATE DESC");

            return _context.Query<PayPalDonation>(sql);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="donation"></param>
        /// <returns></returns>
        public void AddDonation(PayPalDonation donation)
        {
            _context.Save(donation);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="memberid"></param>
        /// <returns></returns>
        public IEnumerable<PayPalDonation> GetDonationsByMember(int memberid)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            if (_context != null)
            {
                _context.Dispose();
            }
        }

        public PayPalDonation GetDonationById(int donationid)
        {
            var sql = new Sql();
            sql.Select("D.*, M.M_FIRSTNAME AS Firstname,M.M_LASTNAME AS Lastname,M.M_NAME AS Username");
            sql.From("FORUM_DONATION D");
            sql.LeftJoin("FORUM_MEMBERS M").On("D.DONATION_MEMBER=M.MEMBER_ID");
            sql.Where(" DONATION_ID=@0", donationid);

            return _context.SingleOrDefault<PayPalDonation>(sql);
        }

        public void DeleteDonation(int donationid)
        {
            _context.Delete<PayPalDonation>(donationid);
        }
    }
}