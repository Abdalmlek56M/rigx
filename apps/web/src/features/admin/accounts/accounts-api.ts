import { unwrap, type Schemas } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";

export type MoneyAccount = Schemas["MoneyAccountView"];
export type MoneyAccountRequest = Schemas["MoneyAccountRequest"];

const api = () => browserApi();
const path = (id: string) => ({ params: { path: { id } } });

/** Every account with its balance, switched-off ones too. */
export const fetchAccounts = () => required(unwrap(api().GET("/api/v1/admin/retail/money/accounts")));

export const createAccount = (body: MoneyAccountRequest) =>
  required(unwrap(api().POST("/api/v1/admin/retail/money/accounts", { body })));

export const updateAccount = (id: string, body: MoneyAccountRequest) =>
  required(unwrap(api().PUT("/api/v1/admin/retail/money/accounts/{id}", { ...path(id), body })));

/** Only an account that was never used; the server refuses (409) otherwise and the account is deactivated instead. */
export const deleteAccount = async (id: string) => {
  await unwrap(api().DELETE("/api/v1/admin/retail/money/accounts/{id}", path(id)));
};
