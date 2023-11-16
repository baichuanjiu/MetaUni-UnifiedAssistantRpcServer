using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Message.RPC.Entities.Message.SystemPromotion
{
    [Table("SystemPromotion")]
    public class SystemPromotion
    {
        [Key]
        public int UUID { get; set; } //UUID，唯一标识，从10000开始计数
        public string Avatar { get; set; } //头像
        public string Name { get; set; } //名称
        public string? MiniAppId { get; set; } //如果是MiniApp的话
        public DateTime UpdatedTime { get; set; } //信息的最后更新时间
    }
}
