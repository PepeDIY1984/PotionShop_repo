using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PotionShopGame : MonoBehaviour
{
    public enum Ingredient { Herb, Mushroom, Crystal, Berry }
    public enum PotionType { Healing, Mana, Speed }

    public Text timeText, coinsText, reputationText, customersText, orderText;
    public Text herbStockText, mushroomStockText, crystalStockText, berryStockText;
    public Text cauldronText, resultText, messageText;
    public Button newDayButton;

    readonly Dictionary<Ingredient,int> inventory=new();
    readonly List<Ingredient> cauldron=new();
    PotionType currentOrder;
    PotionType? brewedPotion;
    int coins,reputation,customersServed;
    float timeLeft;
    bool dayEnded;

    void Start(){ StartNewDay(); }
    void Update(){
        if(!dayEnded){
            timeLeft-=Time.deltaTime;
            if(timeLeft<=0){ timeLeft=0; dayEnded=true; Message("Fin del día."); newDayButton.gameObject.SetActive(true); }
        }
        RefreshUI();
    }

    public void AddHerb()=>Add(Ingredient.Herb);
    public void AddMushroom()=>Add(Ingredient.Mushroom);
    public void AddCrystal()=>Add(Ingredient.Crystal);
    public void AddBerry()=>Add(Ingredient.Berry);

    void Add(Ingredient i){
        if(dayEnded) return;
        if(cauldron.Count>=2){ Message("El caldero ya tiene 2 ingredientes."); return; }
        if(inventory[i]<=0){ Message("No quedan unidades de "+i+"."); return; }
        inventory[i]--; cauldron.Add(i); brewedPotion=null; Message("Añadido: "+i); RefreshUI();
    }

    public void ClearCauldron(){
        foreach(var i in cauldron) inventory[i]++;
        cauldron.Clear(); brewedPotion=null; Message("Ingredientes devueltos al almacén."); RefreshUI();
    }

    public void Brew(){
        if(dayEnded) return;
        if(cauldron.Count!=2){ Message("Necesitas exactamente 2 ingredientes."); return; }
        brewedPotion=Resolve(cauldron[0],cauldron[1]); cauldron.Clear();
        Message(brewedPotion.HasValue ? "Preparada: "+brewedPotion.Value : "La mezcla ha fallado.");
        RefreshUI();
    }

    PotionType? Resolve(Ingredient a,Ingredient b){
        bool Has(Ingredient x,Ingredient y)=>(a==x&&b==y)||(a==y&&b==x);
        if(Has(Ingredient.Herb,Ingredient.Berry)) return PotionType.Healing;
        if(Has(Ingredient.Mushroom,Ingredient.Crystal)) return PotionType.Mana;
        if(Has(Ingredient.Herb,Ingredient.Mushroom)) return PotionType.Speed;
        return null;
    }

    public void Serve(){
        if(dayEnded) return;
        if(!brewedPotion.HasValue){ Message("Primero debes preparar una poción."); return; }
        if(brewedPotion.Value==currentOrder){ coins+=8; reputation++; customersServed++; Message("Pedido correcto. +8 monedas."); }
        else { reputation=Mathf.Max(0,reputation-1); Message("Pedido incorrecto. -1 reputación."); }
        NewCustomer(); RefreshUI();
    }

    public void Restock(){
        if(dayEnded) return;
        if(coins<5){ Message("No tienes monedas suficientes."); return; }
        coins-=5;
        foreach(Ingredient i in Enum.GetValues(typeof(Ingredient))) inventory[i]+=2;
        Message("Reposición: +2 de cada ingrediente."); RefreshUI();
    }

    public void StartNewDay(){
        coins=20; reputation=3; customersServed=0; timeLeft=120; dayEnded=false;
        cauldron.Clear(); brewedPotion=null; inventory.Clear();
        foreach(Ingredient i in Enum.GetValues(typeof(Ingredient))) inventory[i]=5;
        if(newDayButton) newDayButton.gameObject.SetActive(false);
        NewCustomer(); Message("La tienda está abierta."); RefreshUI();
    }

    void NewCustomer(){ currentOrder=(PotionType)UnityEngine.Random.Range(0,3); brewedPotion=null; cauldron.Clear(); }

    void RefreshUI(){
        if(timeText) timeText.text="Tiempo: "+Mathf.CeilToInt(timeLeft)+" s";
        if(coinsText) coinsText.text="Monedas: "+coins;
        if(reputationText) reputationText.text="Reputación: "+reputation;
        if(customersText) customersText.text="Clientes: "+customersServed;
        if(orderText) orderText.text=currentOrder.ToString().ToUpper();
        if(herbStockText) herbStockText.text="x"+inventory.GetValueOrDefault(Ingredient.Herb);
        if(mushroomStockText) mushroomStockText.text="x"+inventory.GetValueOrDefault(Ingredient.Mushroom);
        if(crystalStockText) crystalStockText.text="x"+inventory.GetValueOrDefault(Ingredient.Crystal);
        if(berryStockText) berryStockText.text="x"+inventory.GetValueOrDefault(Ingredient.Berry);
        if(cauldronText) cauldronText.text=cauldron.Count==0?"Vacío":string.Join(" + ",cauldron);
        if(resultText) resultText.text=brewedPotion.HasValue?brewedPotion.Value.ToString():"-";
    }

    void Message(string m){ if(messageText) messageText.text=m; }
}
