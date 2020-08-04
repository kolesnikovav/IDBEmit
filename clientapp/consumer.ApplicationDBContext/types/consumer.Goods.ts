import { consumer.GoodsCategory } from 'types/consumer.GoodsCategory'
import { consumer.VAT } from 'enums/consumer.VAT'
export interface consumer.Goods {
	Id? : number;
	Code? : string;
	Name? : string;
	Category? : consumer.GoodsCategory|number;
	Price? : number;
	VAT? : consumer.VAT;
}
