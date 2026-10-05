Feature: Saints
  As a visitor exploring the faith
  I want to browse the saints catalogue and open a saint's profile
  So that I can read about the lives of the saints

  Scenario: The saints browser lists saint cards
	Given I open the saints browser
	Then I should see at least one saint card

  Scenario: Opening a saint from the saints browser
	Given I open the saints browser
	When I choose the first saint
	Then the saint profile should be visible
	And the saint facts should be visible
